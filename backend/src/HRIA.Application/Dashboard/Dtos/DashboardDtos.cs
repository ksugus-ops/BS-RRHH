using HRIA.Domain.Enums;

namespace HRIA.Application.Dashboard.Dtos;

public record RecentPunchDto(
    string EmployeeName,
    string Department,
    string Action,
    DateTime TimeUtc);

public record DashboardSummaryDto(
    int ActiveEmployees,
    int Working,
    int OnBreak,
    int IncompleteWorkdays,
    int HoursTodayMinutes,
    IReadOnlyList<RecentPunchDto> RecentPunches,
    /// <summary>Minutos previstos hoy por los horarios asignados. 0 si nadie tiene horario.</summary>
    int ExpectedTodayMinutes = 0,
    /// <summary>Empleados que hoy tienen jornada prevista según su horario.</summary>
    int EmployeesScheduledToday = 0,
    /// <summary>Empleados con una ausencia aprobada hoy.</summary>
    int OnLeaveToday = 0,
    /// <summary>Solicitudes de ausencia pendientes de resolver.</summary>
    int PendingAbsenceRequests = 0);

public record HoursByDayPointDto(DateOnly Date, double Hours);

/// <summary>Días de ausencia agregados por tipo, para el gráfico de reparto.</summary>
public record AbsenceByTypeDto(
    string Code,
    string Name,
    string? ColorHex,
    decimal Days,
    int Requests);

/// <summary>Saldo de vacaciones de toda la plantilla en el año en curso.</summary>
public record VacationSummaryDto(
    decimal AllowanceDays,
    decimal ApprovedDays,
    decimal PendingDays,
    decimal AvailableDays);

/// <summary>
/// Ausencia que cae dentro de la ventana de dos semanas, con los días
/// laborables que consume en cada una.
/// </summary>
public record UpcomingAbsenceDto(
    int EmployeeId,
    string EmployeeName,
    string DepartmentName,
    string AbsenceTypeName,
    string AbsenceTypeCode,
    string? ColorHex,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DaysThisWeek,
    decimal DaysNextWeek,
    AbsenceStatus Status);

/// <summary>
/// Totales del mes en curso: días de trabajo, de vacaciones y de otras
/// ausencias, contados en días laborables de empleado.
/// </summary>
public record MonthActivityDto(
    int Year,
    int Month,
    decimal WorkedDays,
    decimal VacationDays,
    decimal OtherAbsenceDays);

/// <summary>
/// Puntualidad del mes: jornadas fichadas dentro y fuera del horario asignado.
/// Solo entran las jornadas cerradas de empleados con horario ese día; el resto
/// no son comparables y quedan fuera del cómputo.
/// </summary>
public record PunctualityDto(
    int Year,
    int Month,
    int ToleranceMinutes,
    int OnScheduleCount,
    int OffScheduleCount,
    int LateInCount,
    int EarlyOutCount,
    double OnSchedulePercent);

/// <summary>Semana actual y siguiente, con quién falta en cada una.</summary>
public record UpcomingAbsencesDto(
    DateOnly ThisWeekStart,
    DateOnly ThisWeekEnd,
    DateOnly NextWeekStart,
    DateOnly NextWeekEnd,
    IReadOnlyList<UpcomingAbsenceDto> Absences);
