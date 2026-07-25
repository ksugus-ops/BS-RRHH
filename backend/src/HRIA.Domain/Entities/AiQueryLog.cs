namespace HRIA.Domain.Entities;

public class AiQueryLog
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Question { get; set; } = string.Empty;
    public string? ToolsUsed { get; set; }

    /// <summary>Success / Denied / ProviderError / Demo.</summary>
    public string ResponseStatus { get; set; } = string.Empty;

    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
