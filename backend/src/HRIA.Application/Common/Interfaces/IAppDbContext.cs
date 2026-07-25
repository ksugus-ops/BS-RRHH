using HRIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Common.Interfaces;

/// <summary>
/// Abstracción del contexto de datos usada por los servicios de aplicación.
/// La implementación concreta (EF Core) vive en Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<User> Users { get; }
    DbSet<Workday> Workdays { get; }
    DbSet<Break> Breaks { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AiQueryLog> AiQueryLogs { get; }

    // --- Horarios, ausencias y vacaciones ---
    DbSet<Schedule> Schedules { get; }
    DbSet<ScheduleSlot> ScheduleSlots { get; }
    DbSet<ScheduleAssignment> ScheduleAssignments { get; }
    DbSet<AbsenceType> AbsenceTypes { get; }
    DbSet<AbsenceRequest> AbsenceRequests { get; }
    DbSet<VacationAllowance> VacationAllowances { get; }
    DbSet<WorkCalendar> WorkCalendars { get; }
    DbSet<Holiday> Holidays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
