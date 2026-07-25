using System.Security.Cryptography;
using HRIA.Application.Auth.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Auth;

public class AuthService : IAuthService
{
    private const string GenericAuthError = "Credenciales inválidas.";

    // Hash "señuelo" para igualar el tiempo de respuesta cuando el usuario no existe
    // (mitiga enumeración de usuarios por temporización). Corresponde a una contraseña aleatoria.
    private const string DummyHash =
        "100000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    /// <summary>Longitud mínima, la misma que exige el alta de empleados.</summary>
    public const int MinPasswordLength = 8;

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ICurrentUser _currentUser;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Employee)!.ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            // Verificación señuelo para no revelar si el correo existe.
            _hasher.Verify(request.Password, DummyHash);
            throw AppException.Unauthorized(GenericAuthError);
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw AppException.Unauthorized(GenericAuthError);

        // Usuario o empleado inactivos: acceso denegado (mensaje genérico).
        if (!user.IsActive || user.Employee is { IsActive: false })
            throw AppException.Unauthorized(GenericAuthError);

        var (token, expiresAt) = _jwt.Generate(user);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "Login",
            Entity = nameof(User),
            EntityId = user.Id.ToString(),
            Details = "Inicio de sesión correcto.",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return new LoginResponse(token, expiresAt, Map(user));
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Employee)!.ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.IsActive)
            throw AppException.Unauthorized("Sesión no válida.");

        return Map(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? throw AppException.Unauthorized("Sesión no válida.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !user.IsActive)
            throw AppException.Unauthorized("Sesión no válida.");

        // Sin comprobar la actual, quien robase un token podría apropiarse de la
        // cuenta cambiando la contraseña.
        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw AppException.BadRequest("La contraseña actual no es correcta.");

        if (request.NewPassword == request.CurrentPassword)
            throw AppException.BadRequest("La nueva contraseña debe ser distinta de la actual.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Se registra el hecho, nunca la contraseña.
        Audit(user.Id, "ChangePassword", user.Id.ToString(), "El usuario cambió su contraseña.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(int employeeId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId, ct)
            ?? throw AppException.NotFound("El empleado no tiene usuario de acceso.");

        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword)
            ? GenerateTemporaryPassword()
            : request.NewPassword.Trim();

        if (newPassword.Length < MinPasswordLength)
            throw AppException.BadRequest($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");

        user.PasswordHash = _hasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        Audit(_currentUser.UserId ?? 0, "ResetPassword", user.Id.ToString(),
            $"Restablecida la contraseña de {user.Email}.");
        await _db.SaveChangesAsync(ct);

        // Única vez que la contraseña viaja en claro: de vuelta a quien la ha
        // restablecido, para que pueda comunicarla. No se almacena.
        return new ResetPasswordResponse(user.Email, newPassword);
    }

    /// <summary>
    /// Contraseña temporal legible pero aleatoria. Se generan los caracteres con
    /// el generador criptográfico, no con Random, y se excluyen los que se
    /// confunden al dictarla (l, I, 1, O, 0).
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(12);
        var body = string.Concat(bytes.Select(b => alphabet[b % alphabet.Length]));
        // El sufijo garantiza dígito y símbolo sin depender del azar.
        return $"Hria-{body}-7";
    }

    private void Audit(int userId, string action, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = nameof(User),
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static CurrentUserDto Map(User user) => new(
        user.Id,
        user.EmployeeId,
        user.Email,
        user.Role,
        user.Employee?.FullName ?? user.Email,
        user.Employee?.Department?.Name,
        user.AvatarUrl);
}
