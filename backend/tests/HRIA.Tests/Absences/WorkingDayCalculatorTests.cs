using HRIA.Application.Absences;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;

namespace HRIA.Tests.Absences;

public class WorkingDayCalculatorTests
{
    private static AppDbContext SeedDb(bool withCalendar = true)
    {
        var db = TestDb.Create();
        var now = DateTime.UtcNow;

        db.Departments.Add(new Department { Id = 1, Name = "Desarrollo", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1,
            FirstName = "Eva",
            LastName = "Empleada",
            Email = "eva@hria.local",
            DepartmentId = 1,
            Position = "Dev",
            HireDate = new DateOnly(2024, 1, 1),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        if (withCalendar)
        {
            db.WorkCalendars.Add(new WorkCalendar
            {
                Id = 1,
                Year = 2026,
                Name = "2026",
                NonWorkingWeekDaysMask = WorkCalendar.DefaultNonWorkingMask,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        db.SaveChanges();
        return db;
    }

    private static void AssignSchedule(AppDbContext db, params DayOfWeek[] days)
    {
        var now = DateTime.UtcNow;
        var schedule = new Schedule
        {
            Id = 1,
            Name = "Horario",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Slots = days.Select(d => new ScheduleSlot
            {
                DayOfWeek = d,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            }).ToList()
        };
        db.Schedules.Add(schedule);
        db.ScheduleAssignments.Add(new ScheduleAssignment
        {
            Id = 1,
            ScheduleId = 1,
            EmployeeId = 1,
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = null,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task CountAsync_SemanaCompleta_ExcluyeSabadoYDomingo()
    {
        var db = SeedDb();
        var calc = new WorkingDayCalculator(db);

        // Lunes 5 a domingo 11 de enero de 2026.
        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.Equal(5m, days);
    }

    [Fact]
    public async Task CountAsync_ConFestivoEntreSemana_LoDescuenta()
    {
        var db = SeedDb();
        db.Holidays.Add(new Holiday
        {
            Id = 1,
            WorkCalendarId = 1,
            Date = new DateOnly(2026, 1, 6),
            Name = "Reyes",
            Kind = HolidayKind.Nacional,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.Equal(4m, days);
    }

    [Fact]
    public async Task CountAsync_FestivoDeConvenio_TambienDescuenta()
    {
        var db = SeedDb();
        db.Holidays.Add(new Holiday
        {
            Id = 1,
            WorkCalendarId = 1,
            Date = new DateOnly(2026, 1, 7),
            Name = "Día de convenio",
            Kind = HolidayKind.Convenio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9));

        Assert.Equal(4m, days);
    }

    [Fact]
    public async Task CountAsync_HorarioDeMediaSemana_SoloCuentaSusDias()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday);
        var calc = new WorkingDayCalculator(db);

        // Semana completa, pero el empleado solo trabaja de lunes a miércoles.
        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.Equal(3m, days);
    }

    [Fact]
    public async Task CountAsync_SinHorarioAsignado_CuentaTodosLosLaborables()
    {
        var db = SeedDb();
        var calc = new WorkingDayCalculator(db);

        // Sin horario no se puede saber qué días trabaja: se aplica solo el
        // calendario, para no dejar a nadie sin poder pedir vacaciones.
        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9));

        Assert.Equal(5m, days);
    }

    [Fact]
    public async Task CountAsync_CentroQueTrabajaSabado_LoCuenta()
    {
        var db = SeedDb(withCalendar: false);
        db.WorkCalendars.Add(new WorkCalendar
        {
            Id = 1,
            Year = 2026,
            Name = "2026",
            NonWorkingWeekDaysMask = 1 << (int)DayOfWeek.Sunday,  // solo domingo
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.Equal(6m, days);
    }

    [Fact]
    public async Task CountAsync_SinCalendarioDelAño_AplicaFinDeSemanaPorDefecto()
    {
        var db = SeedDb(withCalendar: false);
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11));

        Assert.Equal(5m, days);
    }

    [Fact]
    public async Task CountAsync_UnSoloDiaLaborable_DevuelveUno()
    {
        var db = SeedDb();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5));

        Assert.Equal(1m, days);
    }

    [Fact]
    public async Task CountAsync_SoloFinDeSemana_DevuelveCero()
    {
        var db = SeedDb();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11));

        Assert.Equal(0m, days);
    }

    [Fact]
    public async Task CountAsync_FinAnteriorAInicio_DevuelveCero()
    {
        var db = SeedDb();
        var calc = new WorkingDayCalculator(db);

        var days = await calc.CountAsync(1, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 5));

        Assert.Equal(0m, days);
    }
}
