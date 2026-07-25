using HRIA.Application.Schedules.Dtos;

namespace HRIA.Application.Schedules;

public interface IScheduleService
{
    // --- Plantillas de horario ---
    Task<IReadOnlyList<ScheduleListItemDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<ScheduleDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ScheduleDetailDto> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default);
    Task<ScheduleDetailDto> UpdateAsync(int id, UpdateScheduleRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);

    // --- Asignaciones a empleados ---
    Task<IReadOnlyList<ScheduleAssignmentDto>> GetAssignmentsAsync(int? employeeId, int? scheduleId, CancellationToken ct = default);
    Task<ScheduleAssignmentDto> AssignAsync(CreateScheduleAssignmentRequest request, CancellationToken ct = default);
    Task<ScheduleAssignmentDto> UpdateAssignmentAsync(int id, UpdateScheduleAssignmentRequest request, CancellationToken ct = default);
    Task RemoveAssignmentAsync(int id, CancellationToken ct = default);

    /// <summary>Horario vigente de un empleado en una fecha, o null si no tiene ninguno.</summary>
    Task<ScheduleDetailDto?> GetEffectiveScheduleAsync(int employeeId, DateOnly date, CancellationToken ct = default);
}
