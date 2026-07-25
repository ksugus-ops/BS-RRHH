namespace HRIA.Domain.Entities;

/// <summary>
/// Catálogo de tipos de ausencia (vacaciones, enfermedad, asuntos propios…).
/// Las vacaciones son un tipo más, marcado con <see cref="ConsumesVacationBalance"/>,
/// de modo que solicitud y aprobación siguen un único flujo.
/// </summary>
public class AbsenceType
{
    public int Id { get; set; }

    /// <summary>Código estable, apto para lógica de negocio (p. ej. "VACACIONES").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Si es cierto, los días aprobados descuentan del saldo anual de vacaciones.</summary>
    public bool ConsumesVacationBalance { get; set; }

    /// <summary>Si es falso, la solicitud queda aprobada al crearse (p. ej. bajas justificadas).</summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>Color para distinguirlo en el calendario del frontend (#rrggbb).</summary>
    public string? ColorHex { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AbsenceRequest> Requests { get; set; } = new List<AbsenceRequest>();
}
