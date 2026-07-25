using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Common.Models;
using HRIA.Application.Employees.Dtos;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;

    public EmployeeService(IAppDbContext db, IPasswordHasher hasher, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetPagedAsync(EmployeeQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Employees
            .Include(e => e.Department)
            .Include(e => e.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Búsqueda case-insensitive compatible con SQL Server, SQLite e In-Memory.
            var term = query.Search.Trim().ToLower();
            q = q.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term));
        }

        if (query.DepartmentId is > 0)
            q = q.Where(e => e.DepartmentId == query.DepartmentId);

        if (query.IsActive is not null)
            q = q.Where(e => e.IsActive == query.IsActive);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.Id,
                e.FirstName + " " + e.LastName,
                e.Email,
                e.DepartmentId,
                e.Department!.Name,
                e.Position,
                e.IsActive,
                e.User != null ? e.User.Role : (Role?)null))
            .ToListAsync(ct);

        return new PagedResult<EmployeeListItemDto>(items, page, pageSize, total);
    }

    public async Task<EmployeeDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        // Protección horizontal: un empleado solo puede consultar su propia ficha.
        if (_currentUser.Role == Role.Employee && _currentUser.EmployeeId != id)
            throw AppException.Forbidden();

        var e = await _db.Employees
            .Include(x => x.Department)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Empleado no encontrado.");

        return MapDetail(e);
    }

    public async Task<EmployeeDetailDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Employees.AnyAsync(e => e.Email == email, ct) ||
            await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw AppException.Conflict("Ya existe un empleado con ese correo.");

        if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
            throw AppException.BadRequest("El departamento indicado no existe.");

        var now = DateTime.UtcNow;
        var employee = new Employee
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            DepartmentId = request.DepartmentId,
            Position = request.Position.Trim(),
            HireDate = request.HireDate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            User = new User
            {
                Email = email,
                PasswordHash = _hasher.Hash(request.InitialPassword),
                Role = request.Role,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _db.Employees.Add(employee);
        Audit("CreateEmployee", employee.Email, $"Alta de empleado ({request.Role}).");
        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(employee.Id, ct);
    }

    public async Task<EmployeeDetailDto> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw AppException.NotFound("Empleado no encontrado.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Employees.AnyAsync(e => e.Email == email && e.Id != id, ct) ||
            await _db.Users.AnyAsync(u => u.Email == email && u.EmployeeId != id, ct))
            throw AppException.Conflict("Ya existe un empleado con ese correo.");

        if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct))
            throw AppException.BadRequest("El departamento indicado no existe.");

        var now = DateTime.UtcNow;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.DepartmentId = request.DepartmentId;
        employee.Position = request.Position.Trim();
        employee.HireDate = request.HireDate;
        employee.UpdatedAt = now;

        if (employee.User is not null)
        {
            employee.User.Email = email;
            employee.User.Role = request.Role;
            employee.User.UpdatedAt = now;
        }

        Audit("UpdateEmployee", employee.Id.ToString(), "Modificación de empleado.");
        await _db.SaveChangesAsync(ct);

        return await GetByIdInternalAsync(employee.Id, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw AppException.NotFound("Empleado no encontrado.");

        var now = DateTime.UtcNow;
        employee.IsActive = false;
        employee.UpdatedAt = now;
        if (employee.User is not null)
        {
            employee.User.IsActive = false;
            employee.User.UpdatedAt = now;
        }

        Audit("DeactivateEmployee", employee.Id.ToString(), "Baja lógica de empleado.");
        await _db.SaveChangesAsync(ct);
    }

    private async Task<EmployeeDetailDto> GetByIdInternalAsync(int id, CancellationToken ct)
    {
        var e = await _db.Employees
            .Include(x => x.Department)
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id, ct);
        return MapDetail(e);
    }

    private void Audit(string action, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId ?? 0,
            Action = action,
            Entity = nameof(Employee),
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static EmployeeDetailDto MapDetail(Employee e) => new(
        e.Id,
        e.FirstName,
        e.LastName,
        e.Email,
        e.DepartmentId,
        e.Department?.Name ?? string.Empty,
        e.Position,
        e.HireDate,
        e.IsActive,
        e.User?.Role,
        e.CreatedAt,
        e.UpdatedAt);
}
