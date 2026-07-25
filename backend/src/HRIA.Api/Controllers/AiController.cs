using HRIA.Application.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly IAiAssistantService _assistant;

    public AiController(IAiAssistantService assistant) => _assistant = assistant;

    /// <summary>Pregunta al asistente de RR. HH. (solo lectura). Limitado por frecuencia.</summary>
    [HttpPost("ask")]
    [EnableRateLimiting("ai")]
    [ProducesResponseType(typeof(AiAskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AiAskResponse>> Ask([FromBody] AiAskRequest request, CancellationToken ct)
        => Ok(await _assistant.AskAsync(request, ct));
}
