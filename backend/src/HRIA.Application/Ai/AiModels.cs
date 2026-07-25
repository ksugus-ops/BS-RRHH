using System.Text.Json;

namespace HRIA.Application.Ai;

/// <summary>Estado del resultado de una consulta al asistente.</summary>
public static class AiStatus
{
    public const string Success = "Success";
    public const string Denied = "Denied";
    public const string ProviderError = "ProviderError";
    public const string Demo = "Demo";
}

/// <summary>Resultado de ejecutar una herramienta.</summary>
public sealed record AiToolResult(string ForModel, string HumanSummary);

/// <summary>
/// Definición de una herramienta autorizada: metadatos para el modelo y el ejecutor,
/// que ya incorpora la validación de argumentos y los filtros de permisos.
/// </summary>
public sealed record AiTool(
    string Name,
    string Description,
    IReadOnlyList<string> Keywords,
    object ParametersSchema,
    Func<JsonElement?, CancellationToken, Task<AiToolResult>> ExecuteAsync);

/// <summary>Petición al proveedor de IA: pregunta saneada + herramientas permitidas.</summary>
public sealed record AiRequest(string Question, IReadOnlyList<AiTool> Tools);

/// <summary>Respuesta del proveedor de IA.</summary>
public sealed record AiResult(string Answer, IReadOnlyList<string> ToolsUsed, string Status);

/// <summary>DTO devuelto por el endpoint del asistente.</summary>
public sealed record AiAskResponse(
    string Answer,
    IReadOnlyList<string> ToolsUsed,
    string Mode,
    string Status);

public sealed record AiAskRequest(string Question);
