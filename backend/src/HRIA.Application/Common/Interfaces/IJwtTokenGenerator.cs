using HRIA.Domain.Entities;

namespace HRIA.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    /// <summary>Genera un access token JWT para el usuario y devuelve el token y su expiración (UTC).</summary>
    (string token, DateTime expiresAtUtc) Generate(User user);
}
