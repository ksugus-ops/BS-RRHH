using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.WorkCalendars.Dtos;
using HRIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.WorkCalendars;

public class WorkCalendarService : IWorkCalendarService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WorkCalendarService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WorkCalendarListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        var calendars = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .OrderByDescending(c => c.Year)
            .ToListAsync(ct);

        return calendars.Select(c => new WorkCalendarListItemDto(
            c.Id,
            c.Year,
            c.Name,
            c.IsActive,
            c.NonWorkingWeekDays.ToList(),
            c.Holidays.Count)).ToList();
    }

    public async Task<WorkCalendarDetailDto> GetByYearAsync(int year, CancellationToken ct = default)
    {
        var calendar = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.Year == year, ct)
            ?? throw AppException.NotFound($"No hay calendario laboral para {year}.");

        return MapDetail(calendar);
    }

    public async Task<WorkCalendarDetailDto> CreateAsync(CreateWorkCalendarRequest request, CancellationToken ct = default)
    {
        if (await _db.WorkCalendars.AnyAsync(c => c.Year == request.Year, ct))
            throw AppException.Conflict($"Ya existe un calendario laboral para {request.Year}.");

        var now = DateTime.UtcNow;
        var calendar = new WorkCalendar
        {
            Year = request.Year,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyNonWorkingWeekDays(calendar, request.NonWorkingWeekDays);

        _db.WorkCalendars.Add(calendar);
        Audit("CreateWorkCalendar", request.Year.ToString(), $"Alta de calendario laboral {request.Year}.");
        await _db.SaveChangesAsync(ct);

        return await GetByYearAsync(calendar.Year, ct);
    }

    public async Task<WorkCalendarDetailDto> UpdateAsync(int id, UpdateWorkCalendarRequest request, CancellationToken ct = default)
    {
        var calendar = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("Calendario laboral no encontrado.");

        calendar.Name = request.Name.Trim();
        calendar.IsActive = request.IsActive;
        calendar.UpdatedAt = DateTime.UtcNow;
        ApplyNonWorkingWeekDays(calendar, request.NonWorkingWeekDays);

        Audit("UpdateWorkCalendar", id.ToString(), $"Modificación del calendario laboral {calendar.Year}.");
        await _db.SaveChangesAsync(ct);

        return MapDetail(calendar);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var calendar = await _db.WorkCalendars.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("Calendario laboral no encontrado.");

        _db.WorkCalendars.Remove(calendar);
        Audit("DeleteWorkCalendar", id.ToString(), $"Eliminación del calendario laboral {calendar.Year}.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HolidayDto> AddHolidayAsync(int calendarId, HolidayInput input, CancellationToken ct = default)
    {
        var calendar = await _db.WorkCalendars.FirstOrDefaultAsync(c => c.Id == calendarId, ct)
            ?? throw AppException.NotFound("Calendario laboral no encontrado.");

        // El festivo debe caer dentro del año del calendario: si no, no se
        // tendría en cuenta al calcular nada y sería un dato huérfano.
        if (input.Date.Year != calendar.Year)
            throw AppException.BadRequest($"La fecha debe pertenecer al año {calendar.Year}.");

        if (await _db.Holidays.AnyAsync(h => h.WorkCalendarId == calendarId && h.Date == input.Date, ct))
            throw AppException.Conflict("Ya hay un festivo en esa fecha.");

        var now = DateTime.UtcNow;
        var holiday = new Holiday
        {
            WorkCalendarId = calendarId,
            Date = input.Date,
            Name = input.Name.Trim(),
            Kind = input.Kind,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Holidays.Add(holiday);
        Audit("AddHoliday", calendar.Year.ToString(), $"Festivo {input.Date:yyyy-MM-dd} ({input.Kind}): {holiday.Name}.");
        await _db.SaveChangesAsync(ct);

        return new HolidayDto(holiday.Id, holiday.Date, holiday.Name, holiday.Kind);
    }

    public async Task RemoveHolidayAsync(int calendarId, int holidayId, CancellationToken ct = default)
    {
        var holiday = await _db.Holidays
            .FirstOrDefaultAsync(h => h.Id == holidayId && h.WorkCalendarId == calendarId, ct)
            ?? throw AppException.NotFound("Festivo no encontrado.");

        _db.Holidays.Remove(holiday);
        Audit("RemoveHoliday", calendarId.ToString(), $"Eliminación del festivo {holiday.Date:yyyy-MM-dd}.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CalendarDayDto>> GetYearDaysAsync(int year, CancellationToken ct = default)
    {
        var calendar = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.Year == year, ct);

        // Si aún no hay calendario para ese año se devuelve la vista con el
        // criterio por defecto (sábado y domingo no laborables) en lugar de un
        // 404: la pantalla anual debe poder pintarse igualmente.
        var mask = calendar?.NonWorkingWeekDaysMask ?? WorkCalendar.DefaultNonWorkingMask;
        var holidays = calendar?.Holidays.ToDictionary(h => h.Date) ?? new Dictionary<DateOnly, Holiday>();

        var days = new List<CalendarDayDto>(366);
        var date = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        while (date <= end)
        {
            var isWeekend = (mask & (1 << (int)date.DayOfWeek)) != 0;
            holidays.TryGetValue(date, out var holiday);

            days.Add(new CalendarDayDto(
                date,
                IsWorkingDay: !isWeekend && holiday is null,
                IsWeekend: isWeekend,
                HolidayName: holiday?.Name,
                HolidayKind: holiday?.Kind));

            date = date.AddDays(1);
        }

        return days;
    }

    // ------------------------------------------------------------------

    private static void ApplyNonWorkingWeekDays(WorkCalendar calendar, IReadOnlyList<DayOfWeek> days)
    {
        days ??= Array.Empty<DayOfWeek>();

        foreach (var d in days)
        {
            if (!Enum.IsDefined(d))
                throw AppException.BadRequest("Día de la semana no válido.");
        }

        if (days.Count == 7)
            throw AppException.BadRequest("No se pueden marcar los siete días como no laborables.");

        calendar.NonWorkingWeekDaysMask = 0;
        foreach (var d in days.Distinct())
            calendar.SetNonWorkingWeekDay(d, true);
    }

    private static WorkCalendarDetailDto MapDetail(WorkCalendar c)
    {
        var holidays = c.Holidays
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Date, h.Name, h.Kind))
            .ToList();

        // Días laborables del año: ni fin de semana ni festivo.
        var working = 0;
        var date = new DateOnly(c.Year, 1, 1);
        var end = new DateOnly(c.Year, 12, 31);
        var holidayDates = c.Holidays.Select(h => h.Date).ToHashSet();
        while (date <= end)
        {
            if (!c.IsNonWorkingWeekDay(date.DayOfWeek) && !holidayDates.Contains(date))
                working++;
            date = date.AddDays(1);
        }

        return new WorkCalendarDetailDto(
            c.Id,
            c.Year,
            c.Name,
            c.IsActive,
            c.NonWorkingWeekDays.ToList(),
            holidays,
            working,
            c.CreatedAt,
            c.UpdatedAt);
    }

    private void Audit(string action, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId ?? 0,
            Action = action,
            Entity = nameof(WorkCalendar),
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }
}
