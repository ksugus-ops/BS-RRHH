using HRIA.Application.Audit;
using HRIA.Application.Audit.Dtos;
using HRIA.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = "AdminOnly")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit) => _audit = audit;

    /// <summary>Registro de auditoría de acciones sensibles (solo administrador).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _audit.GetAuditAsync(from, to, action, page, pageSize, ct));

    /// <summary>Registro de consultas al asistente de IA (solo administrador).</summary>
    [HttpGet("ai")]
    [ProducesResponseType(typeof(PagedResult<AiQueryLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AiQueryLogDto>>> GetAi(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _audit.GetAiQueriesAsync(page, pageSize, ct));
}
