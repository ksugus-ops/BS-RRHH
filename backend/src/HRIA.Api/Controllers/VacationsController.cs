using HRIA.Application.Absences;
using HRIA.Application.Absences.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/vacations")]
[Authorize]
public sealed class VacationsController : ControllerBase
{
    private readonly IAbsenceService _absences;

    public VacationsController(IAbsenceService absences) => _absences = absences;

    /// <summary>
    /// Saldo de vacaciones de un empleado en un año: días concedidos,
    /// aprobados, pendientes de resolver y disponibles. El empleado solo puede
    /// consultar el suyo.
    /// </summary>
    [HttpGet("balance/{employeeId:int}")]
    [ProducesResponseType(typeof(VacationBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VacationBalanceDto>> GetBalance(
        int employeeId, [FromQuery] int? year, CancellationToken ct)
        => Ok(await _absences.GetBalanceAsync(employeeId, year ?? DateTime.UtcNow.Year, ct));

    /// <summary>Saldo de toda la plantilla activa (solo administrador).</summary>
    [HttpGet("balances")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<VacationBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VacationBalanceDto>>> GetAllBalances(
        [FromQuery] int? year, CancellationToken ct)
        => Ok(await _absences.GetAllBalancesAsync(year ?? DateTime.UtcNow.Year, ct));

    /// <summary>
    /// Fija los días de vacaciones de un empleado para un año
    /// (solo administrador). Si ya existía, se sustituye.
    /// </summary>
    [HttpPut("allowance")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(VacationBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VacationBalanceDto>> SetAllowance(
        [FromBody] SetVacationAllowanceRequest request, CancellationToken ct)
        => Ok(await _absences.SetAllowanceAsync(request, ct));
}
