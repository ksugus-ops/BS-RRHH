using HRIA.Domain.Enums;

namespace HRIA.Application.Employees.Dtos;

/// <summary>Parámetros de consulta del listado de empleados.</summary>
public record EmployeeQuery(
    string? Search = null,
    int? DepartmentId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);

public record EmployeeListItemDto(
    int Id,
    string FullName,
    string Email,
    int DepartmentId,
    string DepartmentName,
    string Position,
    bool IsActive,
    Role? Role);

public record EmployeeDetailDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    string DepartmentName,
    string Position,
    DateOnly HireDate,
    bool IsActive,
    Role? Role,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    string Position,
    DateOnly HireDate,
    Role Role,
    string InitialPassword);

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    string Position,
    DateOnly HireDate,
    Role Role);

public record DepartmentDto(int Id, string Name, bool IsActive);
