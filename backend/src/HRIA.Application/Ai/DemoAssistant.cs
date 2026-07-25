namespace HRIA.Application.Ai;

/// <summary>
/// Asistente en MODO DEMO (sin API key). No usa ningún LLM: selecciona la herramienta
/// autorizada más adecuada según palabras clave de la pregunta, la ejecuta y devuelve
/// su resumen. Si ninguna herramienta encaja, responde de forma controlada.
/// </summary>
public sealed class DemoAssistant : IAiAssistant
{
    public string Mode => "demo";
    public bool IsAvailable => true; // siempre disponible como respaldo

    public async Task<AiResult> AskAsync(AiRequest request, CancellationToken ct = default)
    {
        var question = request.Question.ToLowerInvariant();

        // Selecciona la herramienta con más coincidencias de palabras clave.
        var best = request.Tools
            .Select(t => new { Tool = t, Score = t.Keywords.Count(k => question.Contains(k)) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return new AiResult(
                "Puedo ayudarte con información sobre quién está trabajando ahora, jornadas abiertas o " +
                "incompletas y resúmenes de horas por empleado o departamento. ¿Sobre qué te gustaría preguntar? " +
                "(Modo demostración: sin clave de OpenAI configurada.)",
                Array.Empty<string>(),
                AiStatus.Demo);
        }

        // En modo demo no se parsean argumentos libres: se usan los valores por defecto.
        var result = await best.Tool.ExecuteAsync(null, ct);
        var answer = $"{result.HumanSummary}\n\n(Respuesta generada en modo demostración.)";
        return new AiResult(answer, new[] { best.Tool.Name }, AiStatus.Demo);
    }
}
