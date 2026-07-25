namespace HRIA.Domain.Enums;

/// <summary>Origen de un día festivo dentro del calendario laboral.</summary>
public enum HolidayKind
{
    Nacional = 1,
    Autonomico = 2,
    Local = 3,

    /// <summary>Día de descanso pactado en el convenio colectivo.</summary>
    Convenio = 4,

    /// <summary>Cierre de la empresa (puentes, jornadas de mantenimiento…).</summary>
    Empresa = 5,
}
