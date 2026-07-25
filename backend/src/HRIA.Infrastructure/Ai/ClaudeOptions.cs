namespace HRIA.Infrastructure.Ai;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>Versión de la API de Anthropic (cabecera obligatoria "anthropic-version").</summary>
    public string ApiVersion { get; set; } = "2023-06-01";

    /// <summary>Límite de tokens de la respuesta. El asistente responde breve; 1024 sobra.</summary>
    public int MaxTokens { get; set; } = 1024;
}
