using HRIA.Application.Dashboard;
using HRIA.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AdminOnly")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Indicadores generales del día (solo administrador).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct)
        => Ok(await _dashboard.GetSummaryAsync(ct));

    /// <summary>Serie de horas trabajadas por día para el gráfico.</summary>
    [HttpGet("hours-by-day")]
    [ProducesResponseType(typeof(IReadOnlyList<HoursByDayPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HoursByDayPointDto>>> HoursByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? toDate.AddDays(-6);
        return Ok(await _dashboard.GetHoursByDayAsync(fromDate, toDate, ct));
    }

    /// <summary>Días de ausencia aprobados por tipo, para el gráfico de reparto.</summary>
    [HttpGet("absences-by-type")]
    [ProducesResponseType(typeof(IReadOnlyList<AbsenceByTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AbsenceByTypeDto>>> AbsencesByType(
        [FromQuery] int? year, CancellationToken ct)
        => Ok(await _dashboard.GetAbsencesByTypeAsync(year ?? DateTime.UtcNow.Year, ct));

    /// <summary>Saldo de vacaciones agregado de la plantilla activa.</summary>
    [HttpGet("vacation-summary")]
    [ProducesResponseType(typeof(VacationSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VacationSummaryDto>> VacationSummary(
        [FromQuery] int? year, CancellationToken ct)
        => Ok(await _dashboard.GetVacationSummaryAsync(year ?? DateTime.UtcNow.Year, ct));

    /// <summary>Ausencias de la semana actual y la siguiente, con los días de cada una.</summary>
    [HttpGet("upcoming-absences")]
    [ProducesResponseType(typeof(UpcomingAbsencesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpcomingAbsencesDto>> UpcomingAbsences(CancellationToken ct)
        => Ok(await _dashboard.GetUpcomingAbsencesAsync(ct));

    /// <summary>Totales de trabajo, vacaciones y otras ausencias del mes.</summary>
    [HttpGet("month-activity")]
    [ProducesResponseType(typeof(MonthActivityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MonthActivityDto>> MonthActivity(
        [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return Ok(await _dashboard.GetMonthActivityAsync(year ?? now.Year, month ?? now.Month, ct));
    }

    /// <summary>Jornadas fichadas dentro y fuera del horario asignado.</summary>
    [HttpGet("punctuality")]
    [ProducesResponseType(typeof(PunctualityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PunctualityDto>> Punctuality(
        [FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? toleranceMinutes, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return Ok(await _dashboard.GetPunctualityAsync(
            year ?? now.Year, month ?? now.Month, toleranceMinutes ?? 5, ct));
    }
}
