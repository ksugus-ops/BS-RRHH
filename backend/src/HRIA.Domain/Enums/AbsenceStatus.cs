namespace HRIA.Domain.Enums;

/// <summary>Estado de una solicitud de ausencia o vacaciones.</summary>
public enum AbsenceStatus
{
    /// <summary>Solicitada por el empleado, pendiente de decisión.</summary>
    Pending = 1,

    /// <summary>Aprobada por un administrador.</summary>
    Approved = 2,

    /// <summary>Rechazada por un administrador.</summary>
    Rejected = 3,

    /// <summary>Retirada por el propio empleado antes de resolverse.</summary>
    Cancelled = 4,
}
