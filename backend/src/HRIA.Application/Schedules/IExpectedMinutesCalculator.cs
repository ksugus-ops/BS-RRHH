namespace HRIA.Application.Schedules;

/// <summary>
/// Minutos de trabajo <b>previstos</b> por el horario asignado, para poder
/// contrastarlos con los realmente fichados.
/// </summary>
public interface IExpectedMinutesCalculator
{
    /// <summary>
    /// Minutos previstos para cada par (empleado, fecha) del rango.
    /// Solo contiene entradas para los días en que el empleado tiene un horario
    /// vigente; si no lo tiene, la previsión es desconocida y se omite.
    /// </summary>
    Task<IReadOnlyDictionary<(int EmployeeId, DateOnly Date), int>> GetAsync(
        IReadOnlyCollection<int> employeeIds, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Minutos previstos de un empleado en un día, o null si no tiene horario.</summary>
    Task<int?> GetAsync(int employeeId, DateOnly date, CancellationToken ct = default);
}
