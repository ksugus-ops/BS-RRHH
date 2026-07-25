namespace HRIA.Application.Absences;

/// <summary>
/// Calcula cuántos días laborables consume un periodo para un empleado
/// concreto, combinando el calendario laboral de la empresa con el horario
/// que tenga asignado.
/// </summary>
public interface IWorkingDayCalculator
{
    /// <summary>
    /// Días laborables entre <paramref name="start"/> y <paramref name="end"/>,
    /// ambos inclusive.
    /// </summary>
    Task<decimal> CountAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct = default);
}
