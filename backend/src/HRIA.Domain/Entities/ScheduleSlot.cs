namespace HRIA.Domain.Entities;

/// <summary>
/// Tramo de trabajo dentro de un horario: un día de la semana y una franja
/// horaria. Un mismo día puede tener varios tramos (p. ej. mañana y tarde).
/// Las horas son locales del centro de trabajo, no UTC: representan "las 9:00"
/// con independencia del día concreto al que se apliquen.
/// </summary>
public class ScheduleSlot
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Duración del tramo en minutos.</summary>
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    /// <summary>¿Se solapa con otro tramo del mismo día?</summary>
    public bool OverlapsWith(ScheduleSlot other) =>
        DayOfWeek == other.DayOfWeek && StartTime < other.EndTime && other.StartTime < EndTime;
}
