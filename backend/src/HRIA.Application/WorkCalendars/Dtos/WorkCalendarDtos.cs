using HRIA.Domain.Enums;

namespace HRIA.Application.WorkCalendars.Dtos;

public record HolidayDto(
    int Id,
    DateOnly Date,
    string Name,
    HolidayKind Kind);

public record HolidayInput(
    DateOnly Date,
    string Name,
    HolidayKind Kind);

public record WorkCalendarListItemDto(
    int Id,
    int Year,
    string Name,
    bool IsActive,
    IReadOnlyList<DayOfWeek> NonWorkingWeekDays,
    int HolidayCount);

public record WorkCalendarDetailDto(
    int Id,
    int Year,
    string Name,
    bool IsActive,
    IReadOnlyList<DayOfWeek> NonWorkingWeekDays,
    IReadOnlyList<HolidayDto> Holidays,
    int WorkingDaysInYear,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateWorkCalendarRequest(
    int Year,
    string Name,
    IReadOnlyList<DayOfWeek> NonWorkingWeekDays);

public record UpdateWorkCalendarRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<DayOfWeek> NonWorkingWeekDays);

/// <summary>
/// Un día del calendario anual, tal y como lo consume la vista de 12 meses.
/// </summary>
public record CalendarDayDto(
    DateOnly Date,
    bool IsWorkingDay,
    bool IsWeekend,
    string? HolidayName,
    HolidayKind? HolidayKind);
