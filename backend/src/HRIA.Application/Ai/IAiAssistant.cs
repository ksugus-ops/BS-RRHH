namespace HRIA.Application.Ai;

/// <summary>
/// Abstracción del proveedor de IA (independiente del proveedor concreto).
/// Implementaciones: OpenAiAssistant (live) y DemoAssistant (sin API key).
/// </summary>
public interface IAiAssistant
{
    /// <summary>"live" o "demo".</summary>
    string Mode { get; }

    /// <summary>Indica si el proveedor está configurado y disponible.</summary>
    bool IsAvailable { get; }

    Task<AiResult> AskAsync(AiRequest request, CancellationToken ct = default);
}

public interface IAiAssistantService
{
    Task<AiAskResponse> AskAsync(AiAskRequest request, CancellationToken ct = default);
}
