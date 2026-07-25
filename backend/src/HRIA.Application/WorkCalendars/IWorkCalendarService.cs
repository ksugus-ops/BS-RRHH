using HRIA.Application.WorkCalendars.Dtos;

namespace HRIA.Application.WorkCalendars;

public interface IWorkCalendarService
{
    Task<IReadOnlyList<WorkCalendarListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<WorkCalendarDetailDto> GetByYearAsync(int year, CancellationToken ct = default);
    Task<WorkCalendarDetailDto> CreateAsync(CreateWorkCalendarRequest request, CancellationToken ct = default);
    Task<WorkCalendarDetailDto> UpdateAsync(int id, UpdateWorkCalendarRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // --- Festivos ---
    Task<HolidayDto> AddHolidayAsync(int calendarId, HolidayInput input, CancellationToken ct = default);
    Task RemoveHolidayAsync(int calendarId, int holidayId, CancellationToken ct = default);

    /// <summary>Los 365/366 días del año con su condición de laborable, para la vista anual.</summary>
    Task<IReadOnlyList<CalendarDayDto>> GetYearDaysAsync(int year, CancellationToken ct = default);
}
