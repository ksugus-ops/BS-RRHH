using HRIA.Domain.Enums;

namespace HRIA.Domain.Entities;

/// <summary>
/// Solicitud de ausencia o vacaciones de un empleado para un rango de fechas,
/// ambas inclusive.
/// </summary>
public class AbsenceRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int AbsenceTypeId { get; set; }
    public AbsenceType? AbsenceType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Días laborables que consume la solicitud, calculados al crearla a partir
    /// del horario vigente del empleado y de los festivos. Se guarda porque el
    /// horario puede cambiar después y el saldo consumido no debe alterarse.
    /// </summary>
    public decimal WorkingDays { get; set; }

    public AbsenceStatus Status { get; set; } = AbsenceStatus.Pending;

    /// <summary>Motivo indicado por el empleado.</summary>
    public string? Reason { get; set; }

    public DateTime RequestedAt { get; set; }

    // --- Resolución ---
    public DateTime? DecidedAt { get; set; }

    /// <summary>Usuario administrador que aprobó o rechazó la solicitud.</summary>
    public int? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }

    public string? DecisionComment { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Una solicitud cuenta para el saldo si está aprobada o pendiente de resolver.</summary>
    public bool CountsTowardsBalance =>
        Status is AbsenceStatus.Approved or AbsenceStatus.Pending;

    /// <summary>¿Se solapa en fechas con otro periodo?</summary>
    public bool OverlapsWith(DateOnly start, DateOnly end) =>
        StartDate <= end && start <= EndDate;
}
