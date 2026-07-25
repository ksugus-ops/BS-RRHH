namespace HRIA.Application.Schedules.Dtos;

/// <summary>Tramo horario de un día de la semana. Horas locales del centro de trabajo.</summary>
public record ScheduleSlotDto(
    int Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes);

/// <summary>Tramo tal y como llega en una petición de alta o modificación.</summary>
public record ScheduleSlotInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public record ScheduleListItemDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int WeeklyMinutes,
    int SlotCount,
    int AssignedEmployees);

public record ScheduleDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int WeeklyMinutes,
    IReadOnlyList<ScheduleSlotDto> Slots,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateScheduleRequest(
    string Name,
    string? Description,
    IReadOnlyList<ScheduleSlotInput> Slots);

public record UpdateScheduleRequest(
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<ScheduleSlotInput> Slots);

// --- Asignaciones ---

public record ScheduleAssignmentDto(
    int Id,
    int ScheduleId,
    string ScheduleName,
    int EmployeeId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent);

public record CreateScheduleAssignmentRequest(
    int ScheduleId,
    int EmployeeId,
    DateOnly StartDate,
    DateOnly? EndDate);

public record UpdateScheduleAssignmentRequest(
    DateOnly StartDate,
    DateOnly? EndDate);
