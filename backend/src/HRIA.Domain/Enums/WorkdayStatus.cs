namespace HRIA.Domain.Enums;

/// <summary>Estado de una jornada laboral.</summary>
public enum WorkdayStatus
{
    /// <summary>Entrada registrada, sin salida.</summary>
    Open = 1,

    /// <summary>Entrada y salida correctas.</summary>
    Completed = 2,

    /// <summary>Cerrada sin salida válida (jornada incompleta).</summary>
    Incomplete = 3
}
