using HRIA.Application.Schedules;
using HRIA.Application.Schedules.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public sealed class SchedulesController : ControllerBase
{
    private readonly IScheduleService _schedules;

    public SchedulesController(IScheduleService schedules) => _schedules = schedules;

    /// <summary>Listado de horarios (solo administrador).</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduleListItemDto>>> GetAll(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _schedules.GetAllAsync(includeInactive, ct));

    /// <summary>Detalle de un horario con sus tramos (solo administrador).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ScheduleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleDetailDto>> GetById(int id, CancellationToken ct)
        => Ok(await _schedules.GetByIdAsync(id, ct));

    /// <summary>Alta de horario (solo administrador).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ScheduleDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScheduleDetailDto>> Create([FromBody] CreateScheduleRequest request, CancellationToken ct)
    {
        var created = await _schedules.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Modificación de horario (solo administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ScheduleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScheduleDetailDto>> Update(int id, [FromBody] UpdateScheduleRequest request, CancellationToken ct)
        => Ok(await _schedules.UpdateAsync(id, request, ct));

    /// <summary>Baja lógica de horario (solo administrador).</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _schedules.DeactivateAsync(id, ct);
        return NoContent();
    }

    // ------------------------------------------------------------------
    // Asignaciones
    // ------------------------------------------------------------------

    /// <summary>
    /// Asignaciones de horario. El administrador puede filtrar por empleado u
    /// horario; el empleado solo obtiene las suyas.
    /// </summary>
    [HttpGet("assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ScheduleAssignmentDto>>> GetAssignments(
        [FromQuery] int? employeeId, [FromQuery] int? scheduleId, CancellationToken ct)
        => Ok(await _schedules.GetAssignmentsAsync(employeeId, scheduleId, ct));

    /// <summary>Asigna un horario a un empleado (solo administrador).</summary>
    [HttpPost("assignments")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ScheduleAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScheduleAssignmentDto>> Assign(
        [FromBody] CreateScheduleAssignmentRequest request, CancellationToken ct)
        => Ok(await _schedules.AssignAsync(request, ct));

    /// <summary>Modifica las fechas de una asignación (solo administrador).</summary>
    [HttpPut("assignments/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ScheduleAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScheduleAssignmentDto>> UpdateAssignment(
        int id, [FromBody] UpdateScheduleAssignmentRequest request, CancellationToken ct)
        => Ok(await _schedules.UpdateAssignmentAsync(id, request, ct));

    /// <summary>Elimina una asignación (solo administrador).</summary>
    [HttpDelete("assignments/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(int id, CancellationToken ct)
    {
        await _schedules.RemoveAssignmentAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Horario vigente de un empleado en una fecha. El empleado solo puede
    /// consultar el suyo. Devuelve 204 si no tiene horario asignado.
    /// </summary>
    [HttpGet("effective/{employeeId:int}")]
    [ProducesResponseType(typeof(ScheduleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ScheduleDetailDto>> GetEffective(
        int employeeId, [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = await _schedules.GetEffectiveScheduleAsync(employeeId, day, ct);
        return schedule is null ? NoContent() : Ok(schedule);
    }
}
