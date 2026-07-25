using System.Text.Json;
using FluentValidation;
using HRIA.Application.Common.Exceptions;

namespace HRIA.Api.Middleware;

/// <summary>
/// Manejo global de excepciones. Traduce las excepciones de negocio y de validación
/// a un ProblemDetails uniforme y, en producción, oculta los detalles internos.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblem(context, 400, "Validación fallida.",
                errors: ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (AppException ex)
        {
            // Excepción de negocio esperada: no es un error del sistema.
            await WriteProblem(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            // Log sin datos sensibles: mensaje + traceId.
            _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);
            await WriteProblem(context, 500, "Se produjo un error interno.",
                detail: _env.IsDevelopment() ? ex.Message : null);
        }
    }

    private static async Task WriteProblem(
        HttpContext context, int status, string title,
        string? detail = null, IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problem = new
        {
            type = $"https://hria/errors/{status}",
            title,
            status,
            detail,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
