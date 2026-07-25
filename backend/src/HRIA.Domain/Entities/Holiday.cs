using HRIA.Domain.Enums;

namespace HRIA.Domain.Entities;

/// <summary>
/// Día festivo dentro del calendario laboral de un año. No cuenta como día
/// laborable al calcular los días que consume una ausencia, aunque el horario
/// del empleado tenga tramos ese día.
/// </summary>
public class Holiday
{
    public int Id { get; set; }

    public int WorkCalendarId { get; set; }
    public WorkCalendar? WorkCalendar { get; set; }

    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public HolidayKind Kind { get; set; } = HolidayKind.Nacional;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
