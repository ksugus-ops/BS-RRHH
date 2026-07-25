using System.Net.Http.Json;
using System.Text.Json;
using HRIA.Application.Ai;
using Microsoft.Extensions.Options;

namespace HRIA.Infrastructure.Ai;

/// <summary>
/// Proveedor de IA "live" basado en la API de mensajes de Anthropic (Claude) con
/// uso de herramientas. El modelo nunca accede a la base de datos: solo recibe las
/// definiciones de las herramientas autorizadas y decide cuál llamar; el backend
/// ejecuta, valida y aplica los filtros de permiso.
/// </summary>
public sealed class ClaudeAssistant : IAiAssistant
{
    private const int MaxIterations = 4;

    private readonly HttpClient _http;
    private readonly ClaudeOptions _options;

    public ClaudeAssistant(HttpClient http, IOptions<ClaudeOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public string Mode => "live";
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<AiResult> AskAsync(AiRequest request, CancellationToken ct = default)
    {
        var toolsByName = request.Tools.ToDictionary(t => t.Name);
        var usedTools = new List<string>();
        var systemPrompt = AiPrompt.Build();

        // Claude lleva las instrucciones en "system", fuera de la lista de mensajes:
        // así el usuario no puede sobreescribirlas desde su turno.
        var messages = new List<object>
        {
            new { role = "user", content = request.Question }
        };

        var toolDefs = request.Tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.ParametersSchema
        }).ToArray();

        for (var i = 0; i < MaxIterations; i++)
        {
            var payload = new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokens,
                system = systemPrompt,
                messages,
                tools = toolDefs,
                temperature = 0.2
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v1/messages");
            req.Headers.Add("x-api-key", _options.ApiKey);
            req.Headers.Add("anthropic-version", _options.ApiVersion);
            req.Content = JsonContent.Create(payload);

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;

            var stopReason = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : null;
            var content = root.GetProperty("content");

            if (stopReason == "tool_use")
            {
                // Se reinyecta el turno del asistente ENTERO (texto + bloques tool_use):
                // Anthropic exige que cada tool_use tenga su tool_result en el turno siguiente.
                messages.Add(new { role = "assistant", content = CloneContentBlocks(content) });

                var results = new List<object>();
                foreach (var block in content.EnumerateArray())
                {
                    if (block.GetProperty("type").GetString() != "tool_use") continue;

                    var id = block.GetProperty("id").GetString()!;
                    var name = block.GetProperty("name").GetString()!;

                    string toolContent;
                    if (toolsByName.TryGetValue(name, out var tool))
                    {
                        JsonElement? args = null;
                        if (block.TryGetProperty("input", out var input) && input.ValueKind == JsonValueKind.Object)
                            args = input.Clone();

                        var toolResult = await tool.ExecuteAsync(args, ct);
                        usedTools.Add(name);
                        toolContent = toolResult.ForModel;
                    }
                    else
                    {
                        // Herramienta no autorizada: no se ejecuta.
                        toolContent = "{\"error\":\"herramienta no autorizada\"}";
                    }

                    results.Add(new { type = "tool_result", tool_use_id = id, content = toolContent });
                }

                messages.Add(new { role = "user", content = results });
                continue; // vuelve a preguntar al modelo con los resultados
            }

            // Respuesta final: se concatenan los bloques de texto del turno.
            var answer = string.Concat(content.EnumerateArray()
                .Where(b => b.GetProperty("type").GetString() == "text")
                .Select(b => b.GetProperty("text").GetString()));

            return new AiResult(answer, usedTools.Distinct().ToList(), AiStatus.Success);
        }

        return new AiResult(
            "No he podido completar la respuesta. Reformula la pregunta, por favor.",
            usedTools.Distinct().ToList(),
            AiStatus.Success);
    }

    /// <summary>
    /// Copia los bloques del turno del asistente para devolverlos en el historial.
    /// Se serializa el JSON tal cual lo envió el modelo: reconstruirlo campo a campo
    /// perdería propiedades y la API rechazaría el turno.
    /// </summary>
    private static JsonElement CloneContentBlocks(JsonElement content) => content.Clone();
}
