using HRIA.Application.WorkCalendars;
using HRIA.Application.WorkCalendars.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/work-calendars")]
[Authorize]
public sealed class WorkCalendarsController : ControllerBase
{
    private readonly IWorkCalendarService _calendars;

    public WorkCalendarsController(IWorkCalendarService calendars) => _calendars = calendars;

    /// <summary>Calendarios laborales disponibles (solo administrador).</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkCalendarListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkCalendarListItemDto>>> GetAll(CancellationToken ct)
        => Ok(await _calendars.GetAllAsync(ct));

    /// <summary>Calendario laboral de un año con sus festivos (solo administrador).</summary>
    [HttpGet("{year:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(WorkCalendarDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkCalendarDetailDto>> GetByYear(int year, CancellationToken ct)
        => Ok(await _calendars.GetByYearAsync(year, ct));

    /// <summary>
    /// Los días del año con su condición de laborable, festivo o fin de semana.
    /// Accesible a cualquier usuario autenticado: el empleado lo necesita para
    /// consultar el calendario de la empresa. Si el año no tiene calendario
    /// definido, se devuelve el criterio por defecto en lugar de un error.
    /// </summary>
    [HttpGet("{year:int}/days")]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarDayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarDayDto>>> GetYearDays(int year, CancellationToken ct)
        => Ok(await _calendars.GetYearDaysAsync(year, ct));

    /// <summary>Alta de calendario laboral (solo administrador).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(WorkCalendarDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkCalendarDetailDto>> Create(
        [FromBody] CreateWorkCalendarRequest request, CancellationToken ct)
    {
        var created = await _calendars.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByYear), new { year = created.Year }, created);
    }

    /// <summary>Modificación del calendario laboral (solo administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(WorkCalendarDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkCalendarDetailDto>> Update(
        int id, [FromBody] UpdateWorkCalendarRequest request, CancellationToken ct)
        => Ok(await _calendars.UpdateAsync(id, request, ct));

    /// <summary>Elimina un calendario laboral y sus festivos (solo administrador).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _calendars.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Añade un festivo al calendario (solo administrador).</summary>
    [HttpPost("{id:int}/holidays")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(HolidayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HolidayDto>> AddHoliday(
        int id, [FromBody] HolidayInput input, CancellationToken ct)
        => Ok(await _calendars.AddHolidayAsync(id, input, ct));

    /// <summary>Elimina un festivo del calendario (solo administrador).</summary>
    [HttpDelete("{id:int}/holidays/{holidayId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveHoliday(int id, int holidayId, CancellationToken ct)
    {
        await _calendars.RemoveHolidayAsync(id, holidayId, ct);
        return NoContent();
    }
}
