namespace HRIA.Domain.Entities;

/// <summary>
/// Asignación de un horario a un empleado durante un periodo.
/// <see cref="EndDate"/> nula significa vigente indefinidamente.
/// Un empleado no puede tener dos asignaciones solapadas en el tiempo.
/// </summary>
public class ScheduleAssignment
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>¿Está vigente la asignación en la fecha indicada?</summary>
    public bool IsActiveOn(DateOnly date) =>
        StartDate <= date && (EndDate is null || date <= EndDate.Value);

    /// <summary>¿Se solapa en el tiempo con otro periodo?</summary>
    public bool OverlapsWith(DateOnly start, DateOnly? end) =>
        StartDate <= (end ?? DateOnly.MaxValue) && start <= (EndDate ?? DateOnly.MaxValue);
}
