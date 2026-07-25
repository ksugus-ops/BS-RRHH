using HRIA.Application.Absences.Dtos;
using HRIA.Application.Common.Models;

namespace HRIA.Application.Absences;

public interface IAbsenceService
{
    Task<IReadOnlyList<AbsenceTypeDto>> GetTypesAsync(CancellationToken ct = default);

    Task<PagedResult<AbsenceRequestDto>> GetPagedAsync(AbsenceQuery query, CancellationToken ct = default);
    Task<AbsenceRequestDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<AbsenceRequestDto> CreateAsync(CreateAbsenceRequest request, CancellationToken ct = default);
    Task<AbsenceRequestDto> ApproveAsync(int id, DecideAbsenceRequest request, CancellationToken ct = default);
    Task<AbsenceRequestDto> RejectAsync(int id, DecideAbsenceRequest request, CancellationToken ct = default);
    Task<AbsenceRequestDto> CancelAsync(int id, CancellationToken ct = default);

    // --- Vacaciones ---
    Task<VacationBalanceDto> GetBalanceAsync(int employeeId, int year, CancellationToken ct = default);
    Task<IReadOnlyList<VacationBalanceDto>> GetAllBalancesAsync(int year, CancellationToken ct = default);
    Task<VacationBalanceDto> SetAllowanceAsync(SetVacationAllowanceRequest request, CancellationToken ct = default);

    /// <summary>Calendario anual de vacaciones de toda la plantilla (solo administrador).</summary>
    Task<VacationCalendarDto> GetVacationCalendarAsync(int year, CancellationToken ct = default);
}
