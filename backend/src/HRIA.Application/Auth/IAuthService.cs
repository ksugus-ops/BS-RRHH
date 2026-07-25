using HRIA.Application.Auth.Dtos;

namespace HRIA.Application.Auth;

public interface IAuthService
{
    /// <summary>Valida credenciales y devuelve un token JWT. Lanza AppException(401) si fallan.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Devuelve los datos del usuario autenticado actual.</summary>
    Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Cambia la contraseña del usuario autenticado. Exige la actual: sin ella
    /// cualquiera con un token robado podría apropiarse de la cuenta.
    /// </summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Restablece la contraseña de un empleado (solo administrador). Devuelve la
    /// nueva una única vez; no se guarda de forma recuperable.
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(int employeeId, ResetPasswordRequest request, CancellationToken ct = default);
}
