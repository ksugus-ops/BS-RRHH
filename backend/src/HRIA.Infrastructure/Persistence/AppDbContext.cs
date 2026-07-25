using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HRIA.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Workday> Workdays => Set<Workday>();
    public DbSet<Break> Breaks => Set<Break>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiQueryLog> AiQueryLogs => Set<AiQueryLog>();

    // --- Horarios, ausencias y vacaciones ---
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<ScheduleAssignment> ScheduleAssignments => Set<ScheduleAssignment>();
    public DbSet<AbsenceType> AbsenceTypes => Set<AbsenceType>();
    public DbSet<AbsenceRequest> AbsenceRequests => Set<AbsenceRequest>();
    public DbSet<VacationAllowance> VacationAllowances => Set<VacationAllowance>();
    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // Garantiza que todos los DateTime se traten como UTC al leerlos de la BD
    // (SQLite y SQL Server los devuelven con Kind=Unspecified). Así se serializan
    // con sufijo 'Z' y el frontend los convierte correctamente a la zona local.
    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v) { }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // --- Department ---
        b.Entity<Department>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Employee ---
        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(80);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(80);
            e.Property(x => x.Email).IsRequired().HasMaxLength(160);
            e.Property(x => x.Position).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- User ---
        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).IsRequired().HasMaxLength(160);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);
            e.Property(x => x.Role).HasConversion<int>();
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeId).IsUnique();

            e.HasOne(x => x.Employee)
                .WithOne(emp => emp.User)
                .HasForeignKey<User>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Workday ---
        b.Entity<Workday>(e =>
        {
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Notes).HasMaxLength(500);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.Workdays)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // A lo sumo una jornada ABIERTA (Status = Open = 1) por empleado: refuerza BR-01/BR-06.
            // Las jornadas incompletas (Status = 3) también tienen CheckOut nulo, por eso el
            // filtro incluye el estado. Se adapta a la sintaxis del proveedor.
            var openFilter = Database.IsSqlite()
                ? "\"CheckOut\" IS NULL AND \"Status\" = 1"
                : "[CheckOut] IS NULL AND [Status] = 1";
            e.HasIndex(x => x.EmployeeId)
                .HasFilter(openFilter)
                .IsUnique();
        });

        // --- Break ---
        b.Entity<Break>(e =>
        {
            e.HasOne(x => x.Workday)
                .WithMany(w => w.Breaks)
                .HasForeignKey(x => x.WorkdayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AuditLog ---
        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).IsRequired().HasMaxLength(80);
            e.Property(x => x.Entity).IsRequired().HasMaxLength(80);
            e.Property(x => x.EntityId).HasMaxLength(64);
            e.Property(x => x.Details).HasMaxLength(1000);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- AiQueryLog ---
        b.Entity<AiQueryLog>(e =>
        {
            e.Property(x => x.Question).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ToolsUsed).HasMaxLength(256);
            e.Property(x => x.ResponseStatus).IsRequired().HasMaxLength(40);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Schedule ---
        b.Entity<Schedule>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasIndex(x => x.Name).IsUnique();

            e.Ignore(x => x.WeeklyMinutes);
        });

        // --- ScheduleSlot ---
        b.Entity<ScheduleSlot>(e =>
        {
            e.Property(x => x.DayOfWeek).HasConversion<int>();

            e.HasOne(x => x.Schedule)
                .WithMany(s => s.Slots)
                .HasForeignKey(x => x.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ScheduleId, x.DayOfWeek });
            e.Ignore(x => x.DurationMinutes);
        });

        // --- ScheduleAssignment ---
        b.Entity<ScheduleAssignment>(e =>
        {
            e.HasOne(x => x.Schedule)
                .WithMany(s => s.Assignments)
                .HasForeignKey(x => x.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.ScheduleAssignments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // El solapamiento por empleado no se puede expresar con un índice
            // único: se valida en el servicio. El índice acelera esa consulta.
            e.HasIndex(x => new { x.EmployeeId, x.StartDate });
        });

        // --- AbsenceType ---
        b.Entity<AbsenceType>(e =>
        {
            e.Property(x => x.Code).IsRequired().HasMaxLength(40);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.ColorHex).HasMaxLength(7);
            e.HasIndex(x => x.Code).IsUnique();

            // Catálogo maestro, no datos de demostración: la aplicación no
            // funciona sin él, así que viaja en la migración y no en el seeder.
            // La fecha es fija a propósito: con DateTime.UtcNow el modelo
            // cambiaría en cada compilación y EF pediría una migración nueva.
            // Los colores salen de una paleta validada para daltonismo y
            // contraste en modo claro y oscuro (ver docs/data-model.md). El
            // orden de la lista importa: separa el azul del violeta, que en
            // modo oscuro son indistinguibles si quedan contiguos.
            var seeded = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            e.HasData(
                new AbsenceType { Id = 1, Code = "VACACIONES", Name = "Vacaciones", ConsumesVacationBalance = true, RequiresApproval = true, ColorHex = "#1baf7a", IsActive = true, CreatedAt = seeded, UpdatedAt = seeded },
                new AbsenceType { Id = 2, Code = "ENFERMEDAD", Name = "Baja por enfermedad", ConsumesVacationBalance = false, RequiresApproval = false, ColorHex = "#e34948", IsActive = true, CreatedAt = seeded, UpdatedAt = seeded },
                new AbsenceType { Id = 3, Code = "ASUNTOS_PROPIOS", Name = "Asuntos propios", ConsumesVacationBalance = false, RequiresApproval = true, ColorHex = "#eda100", IsActive = true, CreatedAt = seeded, UpdatedAt = seeded },
                new AbsenceType { Id = 4, Code = "PERMISO", Name = "Permiso retribuido", ConsumesVacationBalance = false, RequiresApproval = true, ColorHex = "#2a78d6", IsActive = true, CreatedAt = seeded, UpdatedAt = seeded },
                new AbsenceType { Id = 5, Code = "SIN_SUELDO", Name = "Permiso sin sueldo", ConsumesVacationBalance = false, RequiresApproval = true, ColorHex = "#4a3aa7", IsActive = true, CreatedAt = seeded, UpdatedAt = seeded }
            );
        });

        // --- AbsenceRequest ---
        b.Entity<AbsenceRequest>(e =>
        {
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.DecisionComment).HasMaxLength(500);
            e.Property(x => x.WorkingDays).HasPrecision(5, 2);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.AbsenceRequests)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AbsenceType)
                .WithMany(t => t.Requests)
                .HasForeignKey(x => x.AbsenceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.DecidedByUser)
                .WithMany()
                .HasForeignKey(x => x.DecidedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.EmployeeId, x.StartDate });
            e.HasIndex(x => x.Status);
            e.Ignore(x => x.CountsTowardsBalance);
        });

        // --- VacationAllowance ---
        b.Entity<VacationAllowance>(e =>
        {
            e.Property(x => x.Days).HasPrecision(5, 2);

            e.HasOne(x => x.Employee)
                .WithMany(emp => emp.VacationAllowances)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Una sola asignación por empleado y año.
            e.HasIndex(x => new { x.EmployeeId, x.Year }).IsUnique();
        });

        // --- WorkCalendar ---
        b.Entity<WorkCalendar>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Year).IsUnique();
            e.Ignore(x => x.NonWorkingWeekDays);
        });

        // --- Holiday ---
        b.Entity<Holiday>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.Kind).HasConversion<int>();

            e.HasOne(x => x.WorkCalendar)
                .WithMany(c => c.Holidays)
                .HasForeignKey(x => x.WorkCalendarId)
                .OnDelete(DeleteBehavior.Cascade);

            // Un festivo por fecha dentro de cada calendario.
            e.HasIndex(x => new { x.WorkCalendarId, x.Date }).IsUnique();
        });
    }
}
