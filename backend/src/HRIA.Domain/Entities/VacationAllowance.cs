namespace HRIA.Domain.Entities;

/// <summary>
/// Días de vacaciones asignados a un empleado para un año natural.
/// Un empleado tiene como mucho una asignación por año.
/// </summary>
public class VacationAllowance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int Year { get; set; }

    /// <summary>Días concedidos. Decimal para admitir medias jornadas.</summary>
    public decimal Days { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
