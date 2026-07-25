using HRIA.Application.Absences;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Dashboard.Dtos;
using HRIA.Application.Schedules;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    private readonly IExpectedMinutesCalculator _expectedMinutes;
    private readonly IWorkingDayCalculator _workingDays;

    public DashboardService(
        IAppDbContext db,
        IExpectedMinutesCalculator expectedMinutes,
        IWorkingDayCalculator workingDays)
    {
        _db = db;
        _expectedMinutes = expectedMinutes;
        _workingDays = workingDays;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var activeEmployees = await _db.Employees.CountAsync(e => e.IsActive, ct);

        // Jornadas abiertas (Status=Open) con sus descansos.
        var openWorkdays = await _db.Workdays
            .Include(w => w.Breaks)
            .Where(w => w.CheckOut == null && w.Status == WorkdayStatus.Open)
            .ToListAsync(ct);

        var todayOpen = openWorkdays.Where(w => w.Date == today).ToList();
        var staleOpen = openWorkdays.Count(w => w.Date < today); // abiertas de días previos = incompletas

        var onBreak = todayOpen.Count(w => w.HasOpenBreak);
        var working = todayOpen.Count - onBreak;

        var incompleteStored = await _db.Workdays.CountAsync(w => w.Status == WorkdayStatus.Incomplete, ct);
        var incompleteWorkdays = incompleteStored + staleOpen;

        // Horas trabajadas hoy (jornadas de hoy, calculadas hasta ahora para las abiertas).
        var todayWorkdays = await _db.Workdays
            .Include(w => w.Breaks)
            .Where(w => w.Date == today)
            .ToListAsync(ct);
        var hoursTodayMinutes = (int)todayWorkdays.Sum(w => WorkedHours(w, now, today) * 60);

        // Últimos fichajes: eventos derivados de las jornadas recientes (entrada/salida/descansos).
        // Se amplía la ventana a una semana y el tope a 100 porque el panel los
        // pagina de diez en diez; con 10 no habría segunda página.
        var recentSince = today.AddDays(-7);
        var recentWorkdays = await _db.Workdays
            .Include(w => w.Breaks)
            .Include(w => w.Employee)!.ThenInclude(e => e!.Department)
            .Where(w => w.Date >= recentSince)
            .ToListAsync(ct);

        var events = new List<RecentPunchDto>();
        foreach (var w in recentWorkdays)
        {
            var name = w.Employee?.FullName ?? "—";
            var dept = w.Employee?.Department?.Name ?? "—";
            events.Add(new RecentPunchDto(name, dept, "Entrada", w.CheckIn));
            if (w.CheckOut is not null)
                events.Add(new RecentPunchDto(name, dept, "Salida", w.CheckOut.Value));
            foreach (var b in w.Breaks)
            {
                events.Add(new RecentPunchDto(name, dept, "Inicio descanso", b.StartTime));
                if (b.EndTime is not null)
                    events.Add(new RecentPunchDto(name, dept, "Fin descanso", b.EndTime.Value));
            }
        }

        var recentPunches = events
            .OrderByDescending(e => e.TimeUtc)
            .Take(100)
            .ToList();

        // --- Previsto según los horarios asignados ---
        var activeIds = await _db.Employees.Where(e => e.IsActive).Select(e => e.Id).ToListAsync(ct);
        var expected = await _expectedMinutes.GetAsync(activeIds, today, today, ct);
        var expectedTodayMinutes = expected.Values.Sum();
        var employeesScheduledToday = expected.Values.Count(m => m > 0);

        var onLeaveToday = await _db.AbsenceRequests
            .CountAsync(a => a.Status == AbsenceStatus.Approved
                             && a.StartDate <= today && today <= a.EndDate, ct);

        var pendingAbsenceRequests = await _db.AbsenceRequests
            .CountAsync(a => a.Status == AbsenceStatus.Pending, ct);

        return new DashboardSummaryDto(
            activeEmployees, working, onBreak, incompleteWorkdays, hoursTodayMinutes, recentPunches,
            expectedTodayMinutes, employeesScheduledToday, onLeaveToday, pendingAbsenceRequests);
    }

    public async Task<IReadOnlyList<HoursByDayPointDto>> GetHoursByDayAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var workdays = await _db.Workdays
            .Include(w => w.Breaks)
            .Where(w => w.Date >= from && w.Date <= to)
            .ToListAsync(ct);

        // Suma de horas trabajadas por día (incluye días sin datos para una serie continua).
        var byDay = workdays
            .GroupBy(w => w.Date)
            .ToDictionary(g => g.Key, g => g.Sum(w => WorkedHours(w, now, today)));

        var points = new List<HoursByDayPointDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var hours = byDay.TryGetValue(d, out var h) ? Math.Round(h, 2) : 0d;
            points.Add(new HoursByDayPointDto(d, hours));
        }

        return points;
    }

    public async Task<IReadOnlyList<AbsenceByTypeDto>> GetAbsencesByTypeAsync(int year, CancellationToken ct = default)
    {
        var rows = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => a.Status == AbsenceStatus.Approved && a.StartDate.Year == year)
            .GroupBy(a => new { a.AbsenceType!.Code, a.AbsenceType.Name, a.AbsenceType.ColorHex })
            .Select(g => new AbsenceByTypeDto(
                g.Key.Code,
                g.Key.Name,
                g.Key.ColorHex,
                g.Sum(a => a.WorkingDays),
                g.Count()))
            .ToListAsync(ct);

        return rows.OrderByDescending(r => r.Days).ToList();
    }

    public async Task<VacationSummaryDto> GetVacationSummaryAsync(int year, CancellationToken ct = default)
    {
        var activeIds = await _db.Employees.Where(e => e.IsActive).Select(e => e.Id).ToListAsync(ct);

        var allowance = await _db.VacationAllowances
            .Where(v => v.Year == year && activeIds.Contains(v.EmployeeId))
            .SumAsync(v => (decimal?)v.Days, ct) ?? 0m;

        // Solo cuentan los tipos que consumen saldo, y se imputan al año de la
        // fecha de inicio (una solicitud no puede abarcar dos años naturales).
        var consuming = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => a.AbsenceType!.ConsumesVacationBalance
                        && a.StartDate.Year == year
                        && activeIds.Contains(a.EmployeeId)
                        && (a.Status == AbsenceStatus.Approved || a.Status == AbsenceStatus.Pending))
            .Select(a => new { a.Status, a.WorkingDays })
            .ToListAsync(ct);

        var approved = consuming.Where(a => a.Status == AbsenceStatus.Approved).Sum(a => a.WorkingDays);
        var pending = consuming.Where(a => a.Status == AbsenceStatus.Pending).Sum(a => a.WorkingDays);

        return new VacationSummaryDto(allowance, approved, pending, allowance - approved - pending);
    }

    public async Task<UpcomingAbsencesDto> GetUpcomingAbsencesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Semana ISO: empieza en lunes, que es lo natural en España.
        var offset = ((int)today.DayOfWeek + 6) % 7;
        var thisStart = today.AddDays(-offset);
        var thisEnd = thisStart.AddDays(6);
        var nextStart = thisEnd.AddDays(1);
        var nextEnd = nextStart.AddDays(6);

        var absences = await _db.AbsenceRequests
            .Include(a => a.Employee)!.ThenInclude(e => e!.Department)
            .Include(a => a.AbsenceType)
            .Where(a => (a.Status == AbsenceStatus.Approved || a.Status == AbsenceStatus.Pending)
                        && a.StartDate <= nextEnd && thisStart <= a.EndDate)
            .OrderBy(a => a.StartDate).ThenBy(a => a.Id)
            .ToListAsync(ct);

        var result = new List<UpcomingAbsenceDto>(absences.Count);

        foreach (var a in absences)
        {
            // Se recorta la ausencia a cada semana y se pide el cómputo al
            // mismo calculador que usa el resto de la aplicación, para que los
            // días respeten el calendario laboral y el horario del empleado.
            var thisWeek = await CountInWindowAsync(a, thisStart, thisEnd, ct);
            var nextWeek = await CountInWindowAsync(a, nextStart, nextEnd, ct);

            result.Add(new UpcomingAbsenceDto(
                a.EmployeeId,
                a.Employee?.FullName ?? string.Empty,
                a.Employee?.Department?.Name ?? string.Empty,
                a.AbsenceType?.Name ?? string.Empty,
                a.AbsenceType?.Code ?? string.Empty,
                a.AbsenceType?.ColorHex,
                a.StartDate,
                a.EndDate,
                thisWeek,
                nextWeek,
                a.Status));
        }

        return new UpcomingAbsencesDto(thisStart, thisEnd, nextStart, nextEnd, result);
    }

    public async Task<MonthActivityDto> GetMonthActivityAsync(int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // Cada jornada es un día de trabajo de un empleado.
        var workedDays = await _db.Workdays.CountAsync(w => w.Date >= from && w.Date <= to, ct);

        var absences = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => a.Status == AbsenceStatus.Approved && a.StartDate <= to && from <= a.EndDate)
            .ToListAsync(ct);

        var vacationDays = 0m;
        var otherDays = 0m;

        foreach (var a in absences)
        {
            // Se recorta al mes: una ausencia de agosto a septiembre solo aporta
            // al mes que se está mirando.
            var days = await CountInWindowAsync(a, from, to, ct);
            if (a.AbsenceType?.ConsumesVacationBalance == true) vacationDays += days;
            else otherDays += days;
        }

        return new MonthActivityDto(year, month, workedDays, vacationDays, otherDays);
    }

    public async Task<PunctualityDto> GetPunctualityAsync(int year, int month, int toleranceMinutes = 5, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var tolerance = TimeSpan.FromMinutes(Math.Max(0, toleranceMinutes));

        // Solo jornadas cerradas: sin hora de salida no hay nada que comparar.
        var workdays = await _db.Workdays
            .Where(w => w.Date >= from && w.Date <= to && w.CheckOut != null)
            .Select(w => new { w.EmployeeId, w.Date, w.CheckIn, w.CheckOut })
            .ToListAsync(ct);

        if (workdays.Count == 0)
            return new PunctualityDto(year, month, toleranceMinutes, 0, 0, 0, 0, 0);

        var employeeIds = workdays.Select(w => w.EmployeeId).Distinct().ToList();

        var assignments = await _db.ScheduleAssignments
            .Include(a => a.Schedule)!
                .ThenInclude(s => s!.Slots)
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.StartDate <= to
                        && (a.EndDate == null || from <= a.EndDate))
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

        var tz = WorkCentreTimeZone();

        int onSchedule = 0, offSchedule = 0, lateIn = 0, earlyOut = 0;

        foreach (var w in workdays)
        {
            var assignment = assignments.FirstOrDefault(a => a.EmployeeId == w.EmployeeId && a.IsActiveOn(w.Date));
            var slots = assignment?.Schedule?.Slots.Where(s => s.DayOfWeek == w.Date.DayOfWeek).ToList();
            if (slots is null || slots.Count == 0) continue;   // sin horario ese día: no comparable

            var expectedIn = slots.Min(s => s.StartTime);
            var expectedOut = slots.Max(s => s.EndTime);

            // Los tramos del horario son hora local del centro; los fichajes se
            // guardan en UTC. Sin esta conversión la comparación estaría
            // desplazada por el huso (y por el horario de verano).
            var actualIn = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(w.CheckIn, tz));
            var actualOut = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(w.CheckOut!.Value, tz));

            // Se pasa a TimeSpan antes de restar: la resta de dos TimeOnly
            // nunca es negativa, envuelve por medianoche, y entrar antes de
            // hora se contaría como un retraso de casi 24 horas.
            var late = actualIn.ToTimeSpan() - expectedIn.ToTimeSpan() > tolerance;
            var early = expectedOut.ToTimeSpan() - actualOut.ToTimeSpan() > tolerance;

            if (late) lateIn++;
            if (early) earlyOut++;

            if (late || early) offSchedule++;
            else onSchedule++;
        }

        var total = onSchedule + offSchedule;
        var percent = total == 0 ? 0 : Math.Round(onSchedule * 100.0 / total, 1);

        return new PunctualityDto(year, month, toleranceMinutes, onSchedule, offSchedule, lateIn, earlyOut, percent);
    }

    /// <summary>
    /// Huso horario del centro de trabajo. Se usa el identificador IANA, que
    /// .NET 8 resuelve también en Windows; si no estuviera disponible se cae al
    /// del servidor, que en este despliegue es el mismo.
    /// </summary>
    private static TimeZoneInfo WorkCentreTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Local; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Local; }
    }

    private async Task<decimal> CountInWindowAsync(AbsenceRequest a, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var start = a.StartDate > from ? a.StartDate : from;
        var end = a.EndDate < to ? a.EndDate : to;
        if (end < start) return 0m;
        return await _workingDays.CountAsync(a.EmployeeId, start, end, ct);
    }

    /// <summary>
    /// Horas trabajadas de una jornada para estadísticas. Las jornadas sin salida
    /// (abiertas/incompletas) solo cuentan si son de hoy; en otro caso no aportan horas
    /// fiables (evita contar días enteros por un olvido de salida).
    /// </summary>
    private static double WorkedHours(Workday w, DateTime now, DateOnly today)
    {
        if (w.CheckOut is not null) return w.WorkedDuration(now).TotalHours;
        return w.Date == today ? w.WorkedDuration(now).TotalHours : 0d;
    }
}
