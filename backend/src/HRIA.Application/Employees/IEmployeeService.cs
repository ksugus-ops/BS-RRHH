using HRIA.Application.Common.Models;
using HRIA.Application.Employees.Dtos;

namespace HRIA.Application.Employees;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default);
    Task<EmployeeDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmployeeDetailDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDetailDto> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetActiveAsync(CancellationToken ct = default);
}
