using HRIA.Application.Absences;
using HRIA.Application.Dashboard;
using HRIA.Application.Schedules;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;

namespace HRIA.Tests.Dashboard;

public class PunctualityTests
{
    private const int EmpId = 1;

    /// <summary>
    /// Huso del centro de trabajo, el mismo que usa el servicio. En julio
    /// Europe/Madrid va en UTC+2, así que las 09:00 locales son las 07:00 UTC.
    /// </summary>
    private static TimeZoneInfo Tz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Local; }
    }

    private static DateTime LocalToUtc(int year, int month, int day, int hour, int minute)
        => TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), Tz());

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

        // Horario de 09:00 a 17:00 (hora local) los miércoles.
        db.Schedules.Add(new Schedule
        {
            Id = 1,
            Name = "Oficina",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Slots = new List<ScheduleSlot>
            {
                new() { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) },
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
        return db;
    }

    /// <summary>Jornada del miércoles 1 de julio de 2026, en horas locales.</summary>
    private static void AddWorkday(AppDbContext db, int inHour, int inMin, int outHour, int outMin)
    {
        var now = DateTime.UtcNow;
        db.Workdays.Add(new Workday
        {
            EmployeeId = EmpId,
            Date = new DateOnly(2026, 7, 1),
            CheckIn = LocalToUtc(2026, 7, 1, inHour, inMin),
            CheckOut = LocalToUtc(2026, 7, 1, outHour, outMin),
            Status = WorkdayStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
    }

    private static DashboardService Service(AppDbContext db) =>
        new(db, new ExpectedMinutesCalculator(db), new WorkingDayCalculator(db));

    [Fact]
    public async Task Puntual_DentroDeTolerancia_CuentaComoDentroDeHorario()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 2, 17, 0);   // dos minutos tarde, dentro de ±5

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(1, r.OnScheduleCount);
        Assert.Equal(0, r.OffScheduleCount);
        Assert.Equal(100, r.OnSchedulePercent);
    }

    [Fact]
    public async Task EntradaTarde_FueraDeTolerancia_CuentaComoFuera()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 20, 17, 0);

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(0, r.OnScheduleCount);
        Assert.Equal(1, r.OffScheduleCount);
        Assert.Equal(1, r.LateInCount);
        Assert.Equal(0, r.EarlyOutCount);
    }

    [Fact]
    public async Task SalidaAnticipada_CuentaComoFuera()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 0, 16, 30);

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(1, r.OffScheduleCount);
        Assert.Equal(1, r.EarlyOutCount);
        Assert.Equal(0, r.LateInCount);
    }

    [Fact]
    public async Task EntrarAntesDeHora_NoSePenaliza()
    {
        var db = SeedDb();
        AddWorkday(db, 8, 30, 17, 0);   // media hora antes

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(1, r.OnScheduleCount);
        Assert.Equal(0, r.LateInCount);
    }

    [Fact]
    public async Task SalirMasTarde_NoSePenaliza()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 0, 18, 30);

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(1, r.OnScheduleCount);
        Assert.Equal(0, r.EarlyOutCount);
    }

    [Fact]
    public async Task LaHoraSeCompararEnHoraLocal_NoEnUtc()
    {
        var db = SeedDb();
        // 09:00 locales = 07:00 UTC en julio. Si se comparase el valor UTC
        // contra las 09:00 del horario, saldría dos horas de adelanto.
        AddWorkday(db, 9, 0, 17, 0);

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(1, r.OnScheduleCount);
        Assert.Equal(0, r.OffScheduleCount);
    }

    [Fact]
    public async Task JornadaSinSalida_NoEntraEnElComputo()
    {
        var db = SeedDb();
        var now = DateTime.UtcNow;
        db.Workdays.Add(new Workday
        {
            EmployeeId = EmpId,
            Date = new DateOnly(2026, 7, 1),
            CheckIn = LocalToUtc(2026, 7, 1, 9, 0),
            CheckOut = null,
            Status = WorkdayStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(0, r.OnScheduleCount);
        Assert.Equal(0, r.OffScheduleCount);
    }

    [Fact]
    public async Task EmpleadoSinHorarioEseDia_NoEntraEnElComputo()
    {
        var db = SeedDb();
        var now = DateTime.UtcNow;
        // Jueves 2 de julio: el horario solo tiene tramo los miércoles.
        db.Workdays.Add(new Workday
        {
            EmployeeId = EmpId,
            Date = new DateOnly(2026, 7, 2),
            CheckIn = LocalToUtc(2026, 7, 2, 9, 0),
            CheckOut = LocalToUtc(2026, 7, 2, 17, 0),
            Status = WorkdayStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(0, r.OnScheduleCount);
        Assert.Equal(0, r.OffScheduleCount);
    }

    [Fact]
    public async Task Porcentaje_SeCalculaSobreLasComparables()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 0, 17, 0);     // dentro
        AddWorkday(db, 9, 30, 17, 0);    // fuera
        AddWorkday(db, 9, 1, 17, 0);     // dentro
        AddWorkday(db, 9, 2, 17, 0);     // dentro

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(3, r.OnScheduleCount);
        Assert.Equal(1, r.OffScheduleCount);
        Assert.Equal(75, r.OnSchedulePercent);
    }

    [Fact]
    public async Task ToleranciaConfigurable_CambiaElResultado()
    {
        var db = SeedDb();
        AddWorkday(db, 9, 10, 17, 0);

        var estricta = await Service(db).GetPunctualityAsync(2026, 7, toleranceMinutes: 5);
        var laxa = await Service(db).GetPunctualityAsync(2026, 7, toleranceMinutes: 30);

        Assert.Equal(1, estricta.OffScheduleCount);
        Assert.Equal(1, laxa.OnScheduleCount);
    }

    [Fact]
    public async Task SinJornadas_DevuelveCeros()
    {
        var db = SeedDb();

        var r = await Service(db).GetPunctualityAsync(2026, 7);

        Assert.Equal(0, r.OnScheduleCount);
        Assert.Equal(0, r.OnSchedulePercent);
    }
}
