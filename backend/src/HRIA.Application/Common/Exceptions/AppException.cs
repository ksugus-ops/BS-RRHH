namespace HRIA.Application.Common.Exceptions;

/// <summary>
/// Excepción de negocio con un código de estado HTTP asociado.
/// El middleware global la traduce a una respuesta uniforme.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    // Atajos habituales.
    public static AppException NotFound(string message = "Recurso no encontrado.") => new(404, message);
    public static AppException Forbidden(string message = "No tienes permiso para esta operación.") => new(403, message);
    public static AppException Unauthorized(string message = "Credenciales inválidas.") => new(401, message);
    public static AppException Conflict(string message) => new(409, message);
    public static AppException BadRequest(string message) => new(400, message);
}
