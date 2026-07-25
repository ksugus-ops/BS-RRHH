using HRIA.Domain.Enums;

namespace HRIA.Domain.Entities;

public class Workday
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Día de la jornada (derivado de CheckIn en UTC).</summary>
    public DateOnly Date { get; set; }

    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    public WorkdayStatus Status { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Break> Breaks { get; set; } = new List<Break>();

    /// <summary>¿Hay un descanso abierto (sin hora de fin)?</summary>
    public bool HasOpenBreak => Breaks.Any(b => b.EndTime is null);

    /// <summary>Suma de la duración de los descansos hasta el instante de referencia.</summary>
    public TimeSpan TotalBreakDuration(DateTime asOfUtc)
    {
        var total = TimeSpan.Zero;
        foreach (var b in Breaks)
            total += (b.EndTime ?? asOfUtc) - b.StartTime;
        return total;
    }

    /// <summary>
    /// Tiempo trabajado = (salida − entrada) − descansos.
    /// Para una jornada abierta se calcula hasta <paramref name="asOfUtc"/>.
    /// Nunca devuelve un valor negativo.
    /// </summary>
    public TimeSpan WorkedDuration(DateTime asOfUtc)
    {
        var end = CheckOut ?? asOfUtc;
        var worked = (end - CheckIn) - TotalBreakDuration(asOfUtc);
        return worked < TimeSpan.Zero ? TimeSpan.Zero : worked;
    }
}
