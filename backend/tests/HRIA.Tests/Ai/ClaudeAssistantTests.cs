using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HRIA.Application.Ai;
using HRIA.Infrastructure.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRIA.Tests.Ai;

/// <summary>
/// Verifica el diálogo con la API de Anthropic sin salir a la red: el bucle de
/// herramientas es la parte delicada (cada tool_use exige su tool_result en el
/// turno siguiente) y es donde un error se manifiesta como un 400 del proveedor.
/// </summary>
public class ClaudeAssistantTests
{
    /// <summary>Devuelve respuestas preparadas y guarda los cuerpos enviados.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;
        public List<string> SentBodies { get; } = new();
        public List<HttpRequestMessage> SentRequests { get; } = new();

        public StubHandler(params string[] responses) => _responses = new Queue<string>(responses);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            SentRequests.Add(request);
            SentBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            };
        }
    }

    private static AiTool FakeTool(string name, string forModel, Action<JsonElement?>? onArgs = null) => new(
        name,
        "herramienta de prueba",
        new[] { "prueba" },
        new { type = "object", properties = new { } },
        (args, _) =>
        {
            onArgs?.Invoke(args);
            return Task.FromResult(new AiToolResult(forModel, "resumen"));
        });

    private static ClaudeAssistant Build(StubHandler handler, string apiKey = "sk-test") =>
        new(new HttpClient(handler), Options.Create(new ClaudeOptions { ApiKey = apiKey }));

    private static string TextResponse(string text) => $$"""
        { "stop_reason": "end_turn", "content": [ { "type": "text", "text": "{{text}}" } ] }
        """;

    private static string ToolUseResponse(string id, string name) => $$"""
        { "stop_reason": "tool_use", "content": [
            { "type": "text", "text": "Consulto los datos." },
            { "type": "tool_use", "id": "{{id}}", "name": "{{name}}", "input": { "from": "2026-01-01" } } ] }
        """;

    [Fact]
    public void IsAvailable_SinApiKey_EsFalso()
    {
        Build(new StubHandler(), apiKey: "").IsAvailable.Should().BeFalse();
        Build(new StubHandler()).IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Ask_RespuestaDirecta_DevuelveElTexto_SinHerramientas()
    {
        var handler = new StubHandler(TextResponse("Hola, puedo consultar horas."));
        var res = await Build(handler).AskAsync(new AiRequest("hola", Array.Empty<AiTool>()));

        res.Answer.Should().Be("Hola, puedo consultar horas.");
        res.ToolsUsed.Should().BeEmpty();
        res.Status.Should().Be(AiStatus.Success);
        handler.SentBodies.Should().ContainSingle();
    }

    [Fact]
    public async Task Ask_CabecerasDeAnthropic_SeEnvian()
    {
        var handler = new StubHandler(TextResponse("ok"));
        await Build(handler).AskAsync(new AiRequest("hola", Array.Empty<AiTool>()));

        var req = handler.SentRequests.Single();
        req.Headers.GetValues("x-api-key").Should().ContainSingle().Which.Should().Be("sk-test");
        req.Headers.GetValues("anthropic-version").Should().ContainSingle().Which.Should().Be("2023-06-01");
        req.RequestUri!.ToString().Should().EndWith("/v1/messages");
    }

    [Fact]
    public async Task Ask_InstruccionesVanEnSystem_NoEnLosMensajes()
    {
        var handler = new StubHandler(TextResponse("ok"));
        await Build(handler).AskAsync(new AiRequest("hola", Array.Empty<AiTool>()));

        using var body = JsonDocument.Parse(handler.SentBodies[0]);
        // El prompt de sistema fuera de "messages": el usuario no puede sobreescribirlo.
        body.RootElement.GetProperty("system").GetString().Should().Contain("SOLO LECTURA");
        var messages = body.RootElement.GetProperty("messages");
        messages.GetArrayLength().Should().Be(1);
        messages[0].GetProperty("role").GetString().Should().Be("user");
    }

    [Fact]
    public async Task Ask_ToolUse_EjecutaLaHerramienta_YDevuelveElResultadoConSuId()
    {
        JsonElement? received = null;
        var tool = FakeTool("get_employee_hours_summary", "{\"hours\":42}", a => received = a);
        var handler = new StubHandler(
            ToolUseResponse("toolu_01", "get_employee_hours_summary"),
            TextResponse("Has trabajado 42 horas."));

        var res = await Build(handler).AskAsync(new AiRequest("mis horas", new[] { tool }));

        res.Answer.Should().Be("Has trabajado 42 horas.");
        res.ToolsUsed.Should().ContainSingle().Which.Should().Be("get_employee_hours_summary");

        // Los argumentos del modelo llegan a la herramienta.
        received!.Value.GetProperty("from").GetString().Should().Be("2026-01-01");

        // El segundo turno reinyecta el mensaje del asistente y el tool_result emparejado.
        using var second = JsonDocument.Parse(handler.SentBodies[1]);
        var messages = second.RootElement.GetProperty("messages");
        messages.GetArrayLength().Should().Be(3); // user, assistant(tool_use), user(tool_result)
        messages[1].GetProperty("role").GetString().Should().Be("assistant");

        var result = messages[2].GetProperty("content")[0];
        result.GetProperty("type").GetString().Should().Be("tool_result");
        result.GetProperty("tool_use_id").GetString().Should().Be("toolu_01");
        result.GetProperty("content").GetString().Should().Be("{\"hours\":42}");
    }

    [Fact]
    public async Task Ask_HerramientaNoAutorizada_NoSeEjecuta()
    {
        var executed = false;
        var permitida = FakeTool("get_employee_hours_summary", "{}", _ => executed = true);
        var handler = new StubHandler(
            ToolUseResponse("toolu_02", "borrar_empleados"),
            TextResponse("No puedo hacer eso."));

        var res = await Build(handler).AskAsync(new AiRequest("borra a todos", new[] { permitida }));

        executed.Should().BeFalse();
        res.ToolsUsed.Should().BeEmpty();

        using var second = JsonDocument.Parse(handler.SentBodies[1]);
        var result = second.RootElement.GetProperty("messages")[2].GetProperty("content")[0];
        result.GetProperty("content").GetString().Should().Contain("no autorizada");
    }

    [Fact]
    public async Task Ask_BuclePersistente_SeCortaSinColgarse()
    {
        var tool = FakeTool("get_employee_hours_summary", "{}");
        // El modelo pide herramienta indefinidamente: debe cortar, no iterar sin fin.
        var responses = Enumerable.Range(0, 10)
            .Select(i => ToolUseResponse($"toolu_{i}", "get_employee_hours_summary"))
            .ToArray();

        var res = await Build(new StubHandler(responses)).AskAsync(new AiRequest("mis horas", new[] { tool }));

        res.Answer.Should().Contain("Reformula");
        res.Status.Should().Be(AiStatus.Success);
    }
}
