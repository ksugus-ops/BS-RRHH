namespace HRIA.Application.Common.Security;

/// <summary>Opciones de configuración del JWT (sección "Jwt").</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HRIA";
    public string Audience { get; set; } = "HRIA.Client";
    public string Secret { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;
}
