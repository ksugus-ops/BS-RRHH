using HRIA.Domain.Enums;

namespace HRIA.Application.Absences.Dtos;

public record AbsenceTypeDto(
    int Id,
    string Code,
    string Name,
    bool ConsumesVacationBalance,
    bool RequiresApproval,
    string? ColorHex);

public record AbsenceRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    int AbsenceTypeId,
    string AbsenceTypeName,
    string AbsenceTypeCode,
    string? ColorHex,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal WorkingDays,
    AbsenceStatus Status,
    string? Reason,
    DateTime RequestedAt,
    DateTime? DecidedAt,
    string? DecidedBy,
    string? DecisionComment);

/// <summary>Filtros del listado. El empleado solo obtiene las suyas.</summary>
public record AbsenceQuery(
    int? EmployeeId = null,
    int? AbsenceTypeId = null,
    AbsenceStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// Alta de solicitud. <see cref="EmployeeId"/> solo lo puede indicar un
/// administrador; para un empleado se toma siempre del token.
/// </summary>
public record CreateAbsenceRequest(
    int AbsenceTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    int? EmployeeId = null);

public record DecideAbsenceRequest(string? Comment);

// ------------------------------------------------------------------
// Vacaciones
// ------------------------------------------------------------------

public record VacationBalanceDto(
    int EmployeeId,
    string EmployeeName,
    int Year,
    decimal AllowanceDays,
    decimal ApprovedDays,
    decimal PendingDays,
    decimal AvailableDays);

public record SetVacationAllowanceRequest(
    int EmployeeId,
    int Year,
    decimal Days);

// ------------------------------------------------------------------
// Calendario anual de vacaciones (vista de 12 meses del administrador)
// ------------------------------------------------------------------

/// <summary>Un periodo de ausencia dentro del calendario anual.</summary>
public record CalendarAbsenceDto(
    int Id,
    DateOnly StartDate,
    DateOnly EndDate,
    string AbsenceTypeName,
    string AbsenceTypeCode,
    string? ColorHex,
    AbsenceStatus Status,
    decimal WorkingDays);

/// <summary>Fila del calendario anual: un empleado y sus ausencias del año.</summary>
public record EmployeeYearAbsencesDto(
    int EmployeeId,
    string EmployeeName,
    string DepartmentName,
    IReadOnlyList<CalendarAbsenceDto> Absences);

public record VacationCalendarDto(
    int Year,
    IReadOnlyList<EmployeeYearAbsencesDto> Employees);
