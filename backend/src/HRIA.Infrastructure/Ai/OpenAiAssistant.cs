using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HRIA.Application.Ai;
using Microsoft.Extensions.Options;

namespace HRIA.Infrastructure.Ai;

/// <summary>
/// Proveedor de IA "live" basado en la API de OpenAI (function calling).
/// El modelo nunca accede a la base de datos: solo recibe las definiciones de las
/// herramientas autorizadas y decide cuál llamar; el backend ejecuta y valida.
/// </summary>
public sealed class OpenAiAssistant : IAiAssistant
{
    private const int MaxIterations = 4;

    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;

    public OpenAiAssistant(HttpClient http, IOptions<OpenAiOptions> options)
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

        var messages = new List<object>
        {
            new { role = "system", content = AiPrompt.Build() },
            new { role = "user", content = request.Question }
        };

        var toolDefs = request.Tools.Select(t => new
        {
            type = "function",
            function = new { name = t.Name, description = t.Description, parameters = t.ParametersSchema }
        }).ToArray();

        for (var i = 0; i < MaxIterations; i++)
        {
            var payload = new
            {
                model = _options.Model,
                messages,
                tools = toolDefs,
                tool_choice = "auto",
                temperature = 0.2
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            req.Content = JsonContent.Create(payload);

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

            // ¿El modelo pide ejecutar herramientas?
            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0)
            {
                // Reinyecta el mensaje del asistente con las tool_calls.
                messages.Add(new { role = "assistant", content = (string?)null, tool_calls = CloneToolCalls(toolCalls) });

                foreach (var call in toolCalls.EnumerateArray())
                {
                    var id = call.GetProperty("id").GetString()!;
                    var fn = call.GetProperty("function");
                    var name = fn.GetProperty("name").GetString()!;

                    string content;
                    if (toolsByName.TryGetValue(name, out var tool))
                    {
                        JsonElement? args = null;
                        if (fn.TryGetProperty("arguments", out var argStr) && argStr.ValueKind == JsonValueKind.String)
                        {
                            var raw = argStr.GetString();
                            if (!string.IsNullOrWhiteSpace(raw))
                            {
                                try { args = JsonDocument.Parse(raw!).RootElement.Clone(); }
                                catch { args = null; }
                            }
                        }
                        var toolResult = await tool.ExecuteAsync(args, ct);
                        usedTools.Add(name);
                        content = toolResult.ForModel;
                    }
                    else
                    {
                        // Herramienta no autorizada: no se ejecuta.
                        content = "{\"error\":\"herramienta no autorizada\"}";
                    }

                    messages.Add(new { role = "tool", tool_call_id = id, content });
                }
                continue; // vuelve a preguntar al modelo con los resultados
            }

            // Respuesta final.
            var answer = message.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty;
            return new AiResult(answer, usedTools.Distinct().ToList(), AiStatus.Success);
        }

        return new AiResult(
            "No he podido completar la respuesta. Reformula la pregunta, por favor.",
            usedTools.Distinct().ToList(),
            AiStatus.Success);
    }

    private static object[] CloneToolCalls(JsonElement toolCalls)
    {
        var list = new List<object>();
        foreach (var call in toolCalls.EnumerateArray())
        {
            var fn = call.GetProperty("function");
            list.Add(new
            {
                id = call.GetProperty("id").GetString(),
                type = "function",
                function = new
                {
                    name = fn.GetProperty("name").GetString(),
                    arguments = fn.TryGetProperty("arguments", out var a) ? a.GetString() ?? "{}" : "{}"
                }
            });
        }
        return list.ToArray();
    }
}
