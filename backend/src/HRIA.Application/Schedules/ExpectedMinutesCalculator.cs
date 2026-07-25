using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Schedules;

/// <summary>
/// Calcula los minutos previstos combinando cuatro fuentes, en este orden:
///
///   1. Si el empleado no tiene horario vigente ese día → previsión desconocida
///      (se omite). Devolver cero sería peor: haría parecer que todo lo fichado
///      es exceso de jornada.
///   2. Si el día es no laborable de la semana o festivo → 0 minutos.
///   3. Si el empleado tiene una ausencia aprobada ese día → 0 minutos, para
///      que estar de vacaciones no aparezca como una desviación negativa.
///   4. En otro caso, la suma de los tramos del horario para ese día.
/// </summary>
public class ExpectedMinutesCalculator : IExpectedMinutesCalculator
{
    private readonly IAppDbContext _db;

    public ExpectedMinutesCalculator(IAppDbContext db) => _db = db;

    public async Task<int?> GetAsync(int employeeId, DateOnly date, CancellationToken ct = default)
    {
        var map = await GetAsync(new[] { employeeId }, date, date, ct);
        return map.TryGetValue((employeeId, date), out var minutes) ? minutes : null;
    }

    public async Task<IReadOnlyDictionary<(int EmployeeId, DateOnly Date), int>> GetAsync(
        IReadOnlyCollection<int> employeeIds, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var result = new Dictionary<(int, DateOnly), int>();
        if (employeeIds.Count == 0 || to < from) return result;

        var ids = employeeIds.Distinct().ToList();

        var years = Enumerable.Range(from.Year, to.Year - from.Year + 1).ToList();
        var calendars = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .Where(c => years.Contains(c.Year))
            .ToDictionaryAsync(c => c.Year, ct);

        var holidays = calendars.Values.SelectMany(c => c.Holidays).Select(h => h.Date).ToHashSet();

        var assignments = await _db.ScheduleAssignments
            .Include(a => a.Schedule)!
                .ThenInclude(s => s!.Slots)
            .Where(a => ids.Contains(a.EmployeeId)
                        && a.StartDate <= to
                        && (a.EndDate == null || from <= a.EndDate))
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

        var absences = await _db.AbsenceRequests
            .Where(a => ids.Contains(a.EmployeeId)
                        && a.Status == AbsenceStatus.Approved
                        && a.StartDate <= to && from <= a.EndDate)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        foreach (var employeeId in ids)
        {
            var employeeAssignments = assignments.Where(a => a.EmployeeId == employeeId).ToList();
            var employeeAbsences = absences.Where(a => a.EmployeeId == employeeId).ToList();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var assignment = employeeAssignments.FirstOrDefault(a => a.IsActiveOn(date));
                if (assignment?.Schedule is null) continue;   // previsión desconocida

                var mask = calendars.TryGetValue(date.Year, out var cal)
                    ? cal.NonWorkingWeekDaysMask
                    : WorkCalendar.DefaultNonWorkingMask;

                var noLaborable =
                    (mask & (1 << (int)date.DayOfWeek)) != 0 ||
                    holidays.Contains(date) ||
                    employeeAbsences.Any(a => a.StartDate <= date && date <= a.EndDate);

                result[(employeeId, date)] = noLaborable
                    ? 0
                    : assignment.Schedule.Slots
                        .Where(s => s.DayOfWeek == date.DayOfWeek)
                        .Sum(s => s.DurationMinutes);
            }
        }

        return result;
    }
}
