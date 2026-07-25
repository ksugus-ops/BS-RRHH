namespace HRIA.Domain.Entities;

/// <summary>
/// Plantilla de horario reutilizable (p. ej. "Oficina 9-18", "Turno de mañana").
/// Define los tramos de trabajo por día de la semana y se asigna a empleados
/// mediante <see cref="ScheduleAssignment"/>.
/// </summary>
public class Schedule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ScheduleSlot> Slots { get; set; } = new List<ScheduleSlot>();
    public ICollection<ScheduleAssignment> Assignments { get; set; } = new List<ScheduleAssignment>();

    /// <summary>Minutos de trabajo previstos para un día de la semana.</summary>
    public int ExpectedMinutesFor(DayOfWeek day) =>
        Slots.Where(s => s.DayOfWeek == day).Sum(s => s.DurationMinutes);

    /// <summary>Minutos de trabajo previstos en una semana completa.</summary>
    public int WeeklyMinutes => Slots.Sum(s => s.DurationMinutes);
}
