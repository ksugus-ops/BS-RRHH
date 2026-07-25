namespace HRIA.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    /// <summary>Resumen sin datos sensibles.</summary>
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}
