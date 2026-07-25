using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Enums;

namespace HRIA.Tests.Common;

/// <summary>Implementación de ICurrentUser controlable para pruebas.</summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;
    public int? UserId { get; set; } = 1;
    public int? EmployeeId { get; set; } = 1;
    public Role? Role { get; set; } = HRIA.Domain.Enums.Role.Admin;

    public static FakeCurrentUser Admin(int userId = 1, int employeeId = 1) =>
        new() { UserId = userId, EmployeeId = employeeId, Role = HRIA.Domain.Enums.Role.Admin };

    public static FakeCurrentUser Employee(int userId, int employeeId) =>
        new() { UserId = userId, EmployeeId = employeeId, Role = HRIA.Domain.Enums.Role.Employee };
}
