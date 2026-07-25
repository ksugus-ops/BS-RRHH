using HRIA.Application.Audit.Dtos;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Audit;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _db;

    public AuditService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> GetAuditAsync(
        DateOnly? from, DateOnly? to, string? action, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.AuditLogs.Include(a => a.User).AsQueryable();

        if (from is not null)
            q = q.Where(a => a.CreatedAt >= from.Value.ToDateTime(TimeOnly.MinValue));
        if (to is not null)
            q = q.Where(a => a.CreatedAt < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.User!.Email, a.Action, a.Entity, a.EntityId, a.Details, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AiQueryLogDto>> GetAiQueriesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.AiQueryLogs.Include(a => a.User).AsQueryable();

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AiQueryLogDto(
                a.Id, a.User!.Email, a.Question, a.ToolsUsed, a.ResponseStatus, a.DurationMs, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AiQueryLogDto>(items, page, pageSize, total);
    }
}
