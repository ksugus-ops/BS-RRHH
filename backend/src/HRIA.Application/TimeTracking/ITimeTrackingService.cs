using HRIA.Application.TimeTracking.Dtos;

namespace HRIA.Application.TimeTracking;

public interface ITimeTrackingService
{
    Task<TimeStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<TimeStatusDto> CheckInAsync(CancellationToken ct = default);
    Task<TimeStatusDto> StartBreakAsync(CancellationToken ct = default);
    Task<TimeStatusDto> EndBreakAsync(CancellationToken ct = default);
    Task<WorkdayDto> CheckOutAsync(CancellationToken ct = default);

    /// <summary>
    /// Histórico de jornadas. El empleado solo ve las suyas; el administrador puede
    /// indicar un employeeId distinto.
    /// </summary>
    Task<IReadOnlyList<WorkdayDto>> GetWorkdaysAsync(WorkdayQuery query, CancellationToken ct = default);
}
