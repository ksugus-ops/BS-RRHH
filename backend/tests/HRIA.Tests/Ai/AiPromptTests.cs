using FluentAssertions;
using HRIA.Application.Ai;
using Xunit;

namespace HRIA.Tests.Ai;

public class AiPromptTests
{
    [Fact]
    public void Build_IncluyeLaFechaDeHoy_EnFormatoIso()
    {
        // Sin la fecha, el modelo inventaba rangos de su corpus de entrenamiento
        // y consultaba semanas vacías: la herramienta devolvía cero y el asistente
        // respondía "no tengo información".
        var tz = TimeZoneInfo.Local;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); } catch { /* zona no instalada */ }
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        AiPrompt.Build().Should().Contain(hoy.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void Build_MantieneLasRestriccionesDeSeguridad()
    {
        var prompt = AiPrompt.Build();

        prompt.Should().Contain("SOLO LECTURA");
        prompt.Should().Contain("EXCLUSIVAMENTE las herramientas");
        prompt.Should().Contain("nunca inventes cifras");
    }

    [Fact]
    public void Build_ExigePasarElRangoYDeclararElPeriodo()
    {
        // Omitir el rango hacía que la herramienta aplicase su valor por defecto
        // (7 días) y el modelo lo presentase como "este mes".
        var prompt = AiPrompt.Build();

        prompt.Should().Contain("pásalos en 'from' y 'to'");
        prompt.Should().Contain("indica en tu respuesta el periodo");
    }
}
