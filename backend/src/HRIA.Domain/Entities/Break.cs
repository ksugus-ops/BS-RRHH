namespace HRIA.Domain.Entities;

public class Break
{
    public int Id { get; set; }

    public int WorkdayId { get; set; }
    public Workday? Workday { get; set; }

    public DateTime StartTime { get; set; }

    /// <summary>Null indica un descanso abierto (en curso).</summary>
    public DateTime? EndTime { get; set; }
}
