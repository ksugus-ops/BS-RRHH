using HRIA.Application.Common.Interfaces;
using HRIA.Application.Employees.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Employees;

public class DepartmentService : IDepartmentService
{
    private readonly IAppDbContext _db;

    public DepartmentService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepartmentDto>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.IsActive))
            .ToListAsync(ct);
    }
}
