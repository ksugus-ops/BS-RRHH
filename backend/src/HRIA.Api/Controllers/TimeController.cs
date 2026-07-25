using HRIA.Application.TimeTracking;
using HRIA.Application.TimeTracking.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/time")]
[Authorize]
public sealed class TimeController : ControllerBase
{
    private readonly ITimeTrackingService _time;

    public TimeController(ITimeTrackingService time) => _time = time;

    /// <summary>Estado de fichaje actual del usuario.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TimeStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeStatusDto>> Status(CancellationToken ct)
        => Ok(await _time.GetStatusAsync(ct));

    /// <summary>Registrar entrada.</summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(TimeStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeStatusDto>> CheckIn(CancellationToken ct)
        => Ok(await _time.CheckInAsync(ct));

    /// <summary>Iniciar descanso.</summary>
    [HttpPost("break/start")]
    [ProducesResponseType(typeof(TimeStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeStatusDto>> StartBreak(CancellationToken ct)
        => Ok(await _time.StartBreakAsync(ct));

    /// <summary>Finalizar descanso.</summary>
    [HttpPost("break/end")]
    [ProducesResponseType(typeof(TimeStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TimeStatusDto>> EndBreak(CancellationToken ct)
        => Ok(await _time.EndBreakAsync(ct));

    /// <summary>Registrar salida.</summary>
    [HttpPost("check-out")]
    [ProducesResponseType(typeof(WorkdayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkdayDto>> CheckOut(CancellationToken ct)
        => Ok(await _time.CheckOutAsync(ct));

    /// <summary>Histórico de jornadas (propias; el admin puede filtrar por empleado).</summary>
    [HttpGet("workdays")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkdayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkdayDto>>> Workdays(
        [FromQuery] int? employeeId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Ok(await _time.GetWorkdaysAsync(new WorkdayQuery(employeeId, from, to), ct));
}
