using HRIA.Domain.Enums;

namespace HRIA.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, CurrentUserDto User);

/// <summary>Cambio de contraseña propio: exige la actual para probar identidad.</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// Restablecimiento por un administrador. No pide la contraseña actual porque
/// el administrador no debe conocerla; si no se indica una nueva, se genera.
/// </summary>
public record ResetPasswordRequest(string? NewPassword = null);

/// <summary>
/// Resultado de un restablecimiento. La contraseña se devuelve <b>una sola vez</b>,
/// en el momento de generarla, para que el administrador pueda comunicarla. No
/// queda almacenada de forma recuperable en ningún sitio.
/// </summary>
public record ResetPasswordResponse(string Email, string TemporaryPassword);

public record CurrentUserDto(
    int UserId,
    int EmployeeId,
    string Email,
    Role Role,
    string FullName,
    string? Department,
    string? AvatarUrl = null);
