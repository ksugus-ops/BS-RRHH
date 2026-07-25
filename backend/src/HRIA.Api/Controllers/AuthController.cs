using HRIA.Application.Auth;
using HRIA.Application.Auth.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HRIA.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    /// <summary>Inicia sesión con correo y contraseña. Devuelve un JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Devuelve el usuario autenticado actual.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
    {
        var userId = _currentUser.UserId
                     ?? throw AppException.Unauthorized("Sesión no válida.");
        var user = await _auth.GetCurrentUserAsync(userId, ct);
        return Ok(user);
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado. Exige la actual, de modo
    /// que un token robado no basta para apropiarse de la cuenta. Con límite de
    /// peticiones para que no sirva para adivinar la contraseña actual.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Restablece la contraseña de un empleado (solo administrador). Devuelve la
    /// nueva <b>una única vez</b>, para que se le pueda comunicar: no se guarda
    /// de forma recuperable ni puede consultarse después.
    /// </summary>
    [HttpPost("reset-password/{employeeId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        int employeeId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
        => Ok(await _auth.ResetPasswordAsync(employeeId, request, ct));
}
