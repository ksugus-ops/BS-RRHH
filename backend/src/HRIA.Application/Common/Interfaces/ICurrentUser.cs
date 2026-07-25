using HRIA.Domain.Enums;

namespace HRIA.Application.Common.Interfaces;

/// <summary>
/// Identidad del usuario autenticado en la petición actual.
/// La implementación concreta (basada en HttpContext) vive en la capa API.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    int? EmployeeId { get; }
    Role? Role { get; }
}
