using HRIA.Application.Audit.Dtos;
using HRIA.Application.Common.Models;

namespace HRIA.Application.Audit;

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> GetAuditAsync(
        DateOnly? from, DateOnly? to, string? action, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<AiQueryLogDto>> GetAiQueriesAsync(int page, int pageSize, CancellationToken ct = default);
}
