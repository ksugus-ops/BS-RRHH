using System.Security.Claims;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Enums;

namespace HRIA.Api.Common;

/// <summary>Identidad del usuario autenticado, leída de los claims del JWT.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId => ParseInt(_principal?.FindFirstValue("sub"));

    public int? EmployeeId => ParseInt(_principal?.FindFirstValue("employeeId"));

    public Role? Role
    {
        get
        {
            var value = _principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<Role>(value, out var role) ? role : null;
        }
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var result) ? result : null;
}
