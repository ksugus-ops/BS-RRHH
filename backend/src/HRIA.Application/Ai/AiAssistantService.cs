using System.Diagnostics;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HRIA.Application.Ai;

public sealed class AiAssistantService : IAiAssistantService
{
    private const int MaxQuestionLength = 500;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly AiToolRegistry _registry;
    private readonly IEnumerable<IAiAssistant> _assistants;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IAppDbContext db,
        ICurrentUser currentUser,
        AiToolRegistry registry,
        IEnumerable<IAiAssistant> assistants,
        ILogger<AiAssistantService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _registry = registry;
        _assistants = assistants;
        _logger = logger;
    }

    public async Task<AiAskResponse> AskAsync(AiAskRequest request, CancellationToken ct = default)
    {
        var role = _currentUser.Role ?? throw AppException.Unauthorized("Sesión no válida.");
        var employeeId = _currentUser.EmployeeId ?? throw AppException.Unauthorized("Sesión no válida.");
        var userId = _currentUser.UserId ?? 0;

        var question = Sanitize(request.Question);
        if (question.Length == 0)
            throw AppException.BadRequest("La pregunta no puede estar vacía.");

        // Herramientas autorizadas SOLO según el rol (la autorización no depende del texto).
        var tools = _registry.BuildTools(role, employeeId);

        // Selecciona el proveedor: el primero disponible (live si hay API key; si no, demo).
        var assistant = PickAssistant();

        var sw = Stopwatch.StartNew();
        AiResult result;
        try
        {
            result = await assistant.AskAsync(new AiRequest(question, tools), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo del proveedor de IA.");
            result = new AiResult(
                "El asistente no está disponible en este momento. Inténtalo de nuevo más tarde.",
                Array.Empty<string>(),
                AiStatus.ProviderError);
        }
        sw.Stop();

        await LogQueryAsync(userId, question, result, (int)sw.ElapsedMilliseconds, ct);

        return new AiAskResponse(result.Answer, result.ToolsUsed, assistant.Mode, result.Status);
    }

    private IAiAssistant PickAssistant()
    {
        // Prioriza un proveedor live disponible; el demo actúa de respaldo.
        var live = _assistants.FirstOrDefault(a => a.Mode == "live" && a.IsAvailable);
        return live ?? _assistants.First(a => a.Mode == "demo");
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Elimina caracteres de control y recorta longitud.
        var cleaned = new string(input.Where(c => !char.IsControl(c) || c == ' ').ToArray()).Trim();
        return cleaned.Length > MaxQuestionLength ? cleaned[..MaxQuestionLength] : cleaned;
    }

    private async Task LogQueryAsync(int userId, string question, AiResult result, int durationMs, CancellationToken ct)
    {
        _db.AiQueryLogs.Add(new AiQueryLog
        {
            UserId = userId,
            Question = question,
            ToolsUsed = result.ToolsUsed.Count > 0 ? string.Join(",", result.ToolsUsed) : null,
            ResponseStatus = result.Status,
            DurationMs = durationMs,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
