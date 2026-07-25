using HRIA.Application.Absences;
using HRIA.Application.Absences.Dtos;
using HRIA.Application.Common.Models;
using HRIA.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/absences")]
[Authorize]
public sealed class AbsencesController : ControllerBase
{
    private readonly IAbsenceService _absences;

    public AbsencesController(IAbsenceService absences) => _absences = absences;

    /// <summary>Catálogo de tipos de ausencia activos.</summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<AbsenceTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AbsenceTypeDto>>> GetTypes(CancellationToken ct)
        => Ok(await _absences.GetTypesAsync(ct));

    /// <summary>
    /// Listado paginado de solicitudes. El administrador ve todas y puede
    /// filtrar por empleado; el empleado solo obtiene las suyas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AbsenceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AbsenceRequestDto>>> GetPaged(
        [FromQuery] int? employeeId,
        [FromQuery] int? absenceTypeId,
        [FromQuery] AbsenceStatus? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _absences.GetPagedAsync(
            new AbsenceQuery(employeeId, absenceTypeId, status, from, to, page, pageSize), ct));

    /// <summary>Detalle de una solicitud. El empleado solo puede ver las suyas.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AbsenceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AbsenceRequestDto>> GetById(int id, CancellationToken ct)
        => Ok(await _absences.GetByIdAsync(id, ct));

    /// <summary>
    /// Crea una solicitud. El empleado solo puede solicitar para sí mismo: el
    /// identificador se toma del token y se ignora el del cuerpo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AbsenceRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceRequestDto>> Create([FromBody] CreateAbsenceRequest request, CancellationToken ct)
    {
        var created = await _absences.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Aprueba una solicitud pendiente (solo administrador).</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AbsenceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceRequestDto>> Approve(int id, [FromBody] DecideAbsenceRequest request, CancellationToken ct)
        => Ok(await _absences.ApproveAsync(id, request, ct));

    /// <summary>Rechaza una solicitud pendiente (solo administrador).</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AbsenceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceRequestDto>> Reject(int id, [FromBody] DecideAbsenceRequest request, CancellationToken ct)
        => Ok(await _absences.RejectAsync(id, request, ct));

    /// <summary>Retira una solicitud pendiente. El empleado solo puede retirar las suyas.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(AbsenceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceRequestDto>> Cancel(int id, CancellationToken ct)
        => Ok(await _absences.CancelAsync(id, ct));

    /// <summary>
    /// Calendario anual de vacaciones de toda la plantilla, para la vista de
    /// 12 meses (solo administrador). Incluye aprobadas y pendientes.
    /// </summary>
    [HttpGet("calendar/{year:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(VacationCalendarDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VacationCalendarDto>> GetVacationCalendar(int year, CancellationToken ct)
        => Ok(await _absences.GetVacationCalendarAsync(year, ct));
}
