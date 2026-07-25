using HRIA.Application.Dashboard.Dtos;

namespace HRIA.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HoursByDayPointDto>> GetHoursByDayAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Días de ausencia aprobados por tipo en un año.</summary>
    Task<IReadOnlyList<AbsenceByTypeDto>> GetAbsencesByTypeAsync(int year, CancellationToken ct = default);

    /// <summary>Saldo de vacaciones agregado de la plantilla activa.</summary>
    Task<VacationSummaryDto> GetVacationSummaryAsync(int year, CancellationToken ct = default);

    /// <summary>Quién falta esta semana y la próxima, con los días de cada una.</summary>
    Task<UpcomingAbsencesDto> GetUpcomingAbsencesAsync(CancellationToken ct = default);

    /// <summary>Totales de trabajo, vacaciones y otras ausencias del mes indicado.</summary>
    Task<MonthActivityDto> GetMonthActivityAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Reparto de jornadas fichadas dentro y fuera del horario asignado.</summary>
    Task<PunctualityDto> GetPunctualityAsync(int year, int month, int toleranceMinutes = 5, CancellationToken ct = default);
}
