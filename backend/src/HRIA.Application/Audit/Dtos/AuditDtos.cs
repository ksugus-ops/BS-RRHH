namespace HRIA.Application.Audit.Dtos;

public record AuditLogDto(
    int Id,
    string UserEmail,
    string Action,
    string Entity,
    string? EntityId,
    string? Details,
    DateTime CreatedAt);

public record AiQueryLogDto(
    int Id,
    string UserEmail,
    string Question,
    string? ToolsUsed,
    string ResponseStatus,
    int DurationMs,
    DateTime CreatedAt);
