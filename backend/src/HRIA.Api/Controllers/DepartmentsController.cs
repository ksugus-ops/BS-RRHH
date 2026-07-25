using HRIA.Application.Employees;
using HRIA.Application.Employees.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments) => _departments = departments;

    /// <summary>Lista de departamentos activos (para filtros y formularios).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetActive(CancellationToken ct)
        => Ok(await _departments.GetActiveAsync(ct));
}
