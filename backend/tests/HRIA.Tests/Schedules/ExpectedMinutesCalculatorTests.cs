using HRIA.Application.Schedules;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;

namespace HRIA.Tests.Schedules;

public class ExpectedMinutesCalculatorTests
{
    private const int EmpId = 1;

    private static AppDbContext SeedDb()
    {
        var db = TestDb.Create();
        var now = DateTime.UtcNow;

        db.Departments.Add(new Department { Id = 1, Name = "Desarrollo", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = EmpId,
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
        db.AbsenceTypes.Add(new AbsenceType
        {
            Id = 1,
            Code = "VACACIONES",
            Name = "Vacaciones",
            ConsumesVacationBalance = true,
            RequiresApproval = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        return db;
    }

    /// <summary>Horario de 8 h (9:00–17:00) los días indicados.</summary>
    private static void AssignSchedule(AppDbContext db, params DayOfWeek[] days)
    {
        var now = DateTime.UtcNow;
        db.Schedules.Add(new Schedule
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
        });
        db.ScheduleAssignments.Add(new ScheduleAssignment
        {
            Id = 1,
            ScheduleId = 1,
            EmployeeId = EmpId,
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = null,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAsync_SinHorarioAsignado_DevuelveNull()
    {
        var db = SeedDb();
        var calc = new ExpectedMinutesCalculator(db);

        // Devolver 0 haría parecer que todo lo fichado es exceso de jornada.
        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));

        Assert.Null(minutes);
    }

    [Fact]
    public async Task GetAsync_DiaConTramos_DevuelveLaSumaDeLosTramos()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday);
        var calc = new ExpectedMinutesCalculator(db);

        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));   // lunes

        Assert.Equal(480, minutes);
    }

    [Fact]
    public async Task GetAsync_DiaSinTramosEnElHorario_DevuelveCero()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday);
        var calc = new ExpectedMinutesCalculator(db);

        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 6));   // martes

        Assert.Equal(0, minutes);
    }

    [Fact]
    public async Task GetAsync_FinDeSemana_DevuelveCero()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday, DayOfWeek.Saturday);
        var calc = new ExpectedMinutesCalculator(db);

        // El horario tiene tramo el sábado, pero el calendario lo marca no laborable.
        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 10));

        Assert.Equal(0, minutes);
    }

    [Fact]
    public async Task GetAsync_Festivo_DevuelveCero()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday);
        db.Holidays.Add(new Holiday
        {
            Id = 1,
            WorkCalendarId = 1,
            Date = new DateOnly(2026, 1, 5),
            Name = "Festivo",
            Kind = HolidayKind.Local,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        var calc = new ExpectedMinutesCalculator(db);

        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));

        Assert.Equal(0, minutes);
    }

    [Fact]
    public async Task GetAsync_ConAusenciaAprobada_DevuelveCero()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday);
        var now = DateTime.UtcNow;
        db.AbsenceRequests.Add(new AbsenceRequest
        {
            Id = 1,
            EmployeeId = EmpId,
            AbsenceTypeId = 1,
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 9),
            WorkingDays = 1,
            Status = AbsenceStatus.Approved,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        var calc = new ExpectedMinutesCalculator(db);

        // Estar de vacaciones no debe aparecer como desviación negativa.
        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));

        Assert.Equal(0, minutes);
    }

    [Fact]
    public async Task GetAsync_ConAusenciaSoloPendiente_SigueEsperandoJornada()
    {
        var db = SeedDb();
        AssignSchedule(db, DayOfWeek.Monday);
        var now = DateTime.UtcNow;
        db.AbsenceRequests.Add(new AbsenceRequest
        {
            Id = 1,
            EmployeeId = EmpId,
            AbsenceTypeId = 1,
            StartDate = new DateOnly(2026, 1, 5),
            EndDate = new DateOnly(2026, 1, 9),
            WorkingDays = 1,
            Status = AbsenceStatus.Pending,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        var calc = new ExpectedMinutesCalculator(db);

        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));

        Assert.Equal(480, minutes);
    }

    [Fact]
    public async Task GetAsync_JornadaPartida_SumaLosDosTramos()
    {
        var db = SeedDb();
        var now = DateTime.UtcNow;
        db.Schedules.Add(new Schedule
        {
            Id = 1,
            Name = "Partido",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Slots = new List<ScheduleSlot>
            {
                new() { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(14, 0) },
                new() { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(18, 0) },
            }
        });
        db.ScheduleAssignments.Add(new ScheduleAssignment
        {
            Id = 1,
            ScheduleId = 1,
            EmployeeId = EmpId,
            StartDate = new DateOnly(2020, 1, 1),
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        var calc = new ExpectedMinutesCalculator(db);

        var minutes = await calc.GetAsync(EmpId, new DateOnly(2026, 1, 5));

        Assert.Equal(480, minutes);   // 5 h + 3 h
    }

    [Fact]
    public async Task GetAsync_RangoDeVariosDias_SoloIncluyeLosDiasConHorarioVigente()
    {
        var db = SeedDb();
        var now = DateTime.UtcNow;
        db.Schedules.Add(new Schedule
        {
            Id = 1,
            Name = "Horario",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Slots = new List<ScheduleSlot>
            {
                new() { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) },
            }
        });
        // Vigente solo hasta el 6 de enero.
        db.ScheduleAssignments.Add(new ScheduleAssignment
        {
            Id = 1,
            ScheduleId = 1,
            EmployeeId = EmpId,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 6),
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        var calc = new ExpectedMinutesCalculator(db);

        var map = await calc.GetAsync(new[] { EmpId }, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 13));

        Assert.True(map.ContainsKey((EmpId, new DateOnly(2026, 1, 5))));    // dentro de la vigencia
        Assert.False(map.ContainsKey((EmpId, new DateOnly(2026, 1, 12))));  // ya sin horario
    }
}
