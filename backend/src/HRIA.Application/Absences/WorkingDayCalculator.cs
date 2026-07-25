using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Absences;

/// <summary>
/// Una fecha cuenta como laborable para un empleado si se cumplen las tres
/// condiciones a la vez:
///
///   1. No es un día de la semana no laborable según el calendario del año.
///   2. No es festivo de ese calendario.
///   3. El horario asignado al empleado tiene tramos ese día de la semana.
///
/// Si el empleado no tiene horario asignado se aplican solo las dos primeras:
/// lo contrario daría cero días a quien aún no tiene horario, y nadie podría
/// solicitar vacaciones hasta que se le asignara uno.
/// </summary>
public class WorkingDayCalculator : IWorkingDayCalculator
{
    private readonly IAppDbContext _db;

    public WorkingDayCalculator(IAppDbContext db) => _db = db;

    public async Task<decimal> CountAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        if (end < start) return 0m;

        // Calendarios de los años que abarca el periodo.
        var years = Enumerable.Range(start.Year, end.Year - start.Year + 1).ToList();
        var calendars = await _db.WorkCalendars
            .Include(c => c.Holidays)
            .Where(c => years.Contains(c.Year))
            .ToDictionaryAsync(c => c.Year, ct);

        var holidays = calendars.Values
            .SelectMany(c => c.Holidays)
            .Select(h => h.Date)
            .ToHashSet();

        // Asignaciones de horario que se solapan con el periodo, con sus tramos.
        var assignments = await _db.ScheduleAssignments
            .Include(a => a.Schedule)!
                .ThenInclude(s => s!.Slots)
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= end
                        && (a.EndDate == null || start <= a.EndDate))
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

        var count = 0m;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var mask = calendars.TryGetValue(date.Year, out var cal)
                ? cal.NonWorkingWeekDaysMask
                : WorkCalendar.DefaultNonWorkingMask;

            if ((mask & (1 << (int)date.DayOfWeek)) != 0) continue;   // fin de semana
            if (holidays.Contains(date)) continue;                    // festivo

            // Horario vigente ese día, si lo hay.
            var assignment = assignments.FirstOrDefault(a => a.IsActiveOn(date));
            if (assignment?.Schedule is not null &&
                !assignment.Schedule.Slots.Any(s => s.DayOfWeek == date.DayOfWeek))
                continue;                                             // ese día no trabaja

            count++;
        }

        return count;
    }
}
