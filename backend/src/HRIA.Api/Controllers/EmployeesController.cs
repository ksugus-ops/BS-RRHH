using HRIA.Application.Common.Models;
using HRIA.Application.Employees;
using HRIA.Application.Employees.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees) => _employees = employees;

    /// <summary>Listado paginado de empleados (solo administrador).</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<EmployeeListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _employees.GetPagedAsync(
            new EmployeeQuery(search, departmentId, isActive, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Detalle de un empleado. El administrador ve cualquiera; el empleado solo el suyo.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id, CancellationToken ct)
        => Ok(await _employees.GetByIdAsync(id, ct));

    /// <summary>Alta de empleado (solo administrador).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeDetailDto>> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var created = await _employees.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Modificación de empleado (solo administrador).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeDetailDto>> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
        => Ok(await _employees.UpdateAsync(id, request, ct));

    /// <summary>Baja lógica de empleado (solo administrador).</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _employees.DeactivateAsync(id, ct);
        return NoContent();
    }
}
