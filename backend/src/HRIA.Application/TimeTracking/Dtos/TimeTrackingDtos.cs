using HRIA.Domain.Enums;

namespace HRIA.Application.TimeTracking.Dtos;

/// <summary>Estado actual del fichaje del empleado.</summary>
public enum TimeState
{
    NotStarted = 0,
    Working = 1,
    OnBreak = 2
}

public record BreakDto(int Id, DateTime StartTime, DateTime? EndTime, int DurationMinutes);

public record WorkdayDto(
    int Id,
    int EmployeeId,
    DateOnly Date,
    DateTime CheckIn,
    DateTime? CheckOut,
    WorkdayStatus Status,
    int WorkedMinutes,
    IReadOnlyList<BreakDto> Breaks,
    /// <summary>Minutos previstos por el horario asignado. Null si no tiene horario.</summary>
    int? ExpectedMinutes = null,
    /// <summary>Trabajados − previstos. Negativo = falta jornada. Null si no hay previsión.</summary>
    int? DeviationMinutes = null);

public record TimeStatusDto(TimeState State, WorkdayDto? Workday);

public record WorkdayQuery(int? EmployeeId = null, DateOnly? From = null, DateOnly? To = null);
