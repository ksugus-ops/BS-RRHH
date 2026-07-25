using HRIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    /// <summary>Comprobación de estado del servicio y de la base de datos.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool dbUp;
        try
        {
            dbUp = await _db.Database.CanConnectAsync(ct);
        }
        catch
        {
            dbUp = false;
        }

        var payload = new
        {
            status = dbUp ? "healthy" : "degraded",
            service = "HRIA.Api",
            database = dbUp ? "up" : "down",
            timeUtc = DateTime.UtcNow
        };

        return dbUp ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
}
