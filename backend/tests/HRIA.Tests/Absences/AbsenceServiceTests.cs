using HRIA.Application.Absences;
using HRIA.Application.Absences.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;

namespace HRIA.Tests.Absences;

public class AbsenceServiceTests
{
    private const int Vacaciones = 1;   // consume saldo, requiere aprobación
    private const int Enfermedad = 2;   // no consume saldo, no requiere aprobación

    private static AppDbContext SeedDb()
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
        db.Employees.Add(new Employee
        {
            Id = 2,
            FirstName = "Ana",
            LastName = "Admin",
            Email = "ana@hria.local",
            DepartmentId = 1,
            Position = "RRHH",
            HireDate = new DateOnly(2024, 1, 1),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Users.Add(new User
        {
            Id = 1,
            EmployeeId = 2,
            Email = "ana@hria.local",
            PasswordHash = "x",
            Role = Role.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.AbsenceTypes.Add(new AbsenceType
        {
            Id = Vacaciones,
            Code = "VACACIONES",
            Name = "Vacaciones",
            ConsumesVacationBalance = true,
            RequiresApproval = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.AbsenceTypes.Add(new AbsenceType
        {
            Id = Enfermedad,
            Code = "ENFERMEDAD",
            Name = "Baja por enfermedad",
            ConsumesVacationBalance = false,
            RequiresApproval = false,
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

        db.SaveChanges();
        return db;
    }

    private static AbsenceService ServiceFor(AppDbContext db, FakeCurrentUser user) =>
        new(db, user, new WorkingDayCalculator(db));

    private static AbsenceService AdminService(AppDbContext db) =>
        ServiceFor(db, FakeCurrentUser.Admin(userId: 1, employeeId: 2));

    private static AbsenceService EmployeeService(AppDbContext db) =>
        ServiceFor(db, FakeCurrentUser.Employee(userId: 2, employeeId: 1));

    private static async Task GiveAllowanceAsync(AppDbContext db, decimal days = 23m, int year = 2026)
    {
        await AdminService(db).SetAllowanceAsync(new SetVacationAllowanceRequest(1, year, days));
    }

    // ---------------- Alta ----------------

    [Fact]
    public async Task CreateAsync_Vacaciones_QuedaPendienteYCuentaDiasLaborables()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);

        // Lunes 5 a viernes 9 de enero de 2026.
        var created = await svc.CreateAsync(new CreateAbsenceRequest(
            Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), "Descanso"));

        Assert.Equal(AbsenceStatus.Pending, created.Status);
        Assert.Equal(5m, created.WorkingDays);
        Assert.Equal(1, created.EmployeeId);
    }

    [Fact]
    public async Task CreateAsync_TipoQueNoRequiereAprobacion_NaceAprobada()
    {
        var db = SeedDb();
        var svc = EmployeeService(db);

        var created = await svc.CreateAsync(new CreateAbsenceRequest(
            Enfermedad, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 6), "Gripe"));

        Assert.Equal(AbsenceStatus.Approved, created.Status);
        Assert.NotNull(created.DecidedAt);
    }

    [Fact]
    public async Task CreateAsync_EmpleadoIndicandoOtroEmpleado_SeIgnoraYUsaElDelToken()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);

        // Intenta solicitar en nombre del empleado 2.
        var created = await svc.CreateAsync(new CreateAbsenceRequest(
            Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null, EmployeeId: 2));

        Assert.Equal(1, created.EmployeeId);
    }

    [Fact]
    public async Task CreateAsync_PeriodoSolapadoConOtraSolicitudViva_LanzaConflicto()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);
        await svc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 12), null)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_SolapadoConUnaRechazada_SeAcepta()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var empSvc = EmployeeService(db);
        var first = await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));
        await AdminService(db).RejectAsync(first.Id, new DecideAbsenceRequest("No procede"));

        var second = await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));

        Assert.Equal(AbsenceStatus.Pending, second.Status);
    }

    [Fact]
    public async Task CreateAsync_PeriodoSoloDeFinDeSemana_LanzaBadRequest()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 11), null)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_VacacionesQueCruzanElAño_LanzaBadRequest()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 5), null)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("dos años", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SinSaldoSuficiente_LanzaConflicto()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db, days: 3m);
        var svc = EmployeeService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null)));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("Saldo insuficiente", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SinAsignacionDeDias_ElSaldoEsCeroYNoDejaPedir()
    {
        var db = SeedDb();
        var svc = EmployeeService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null)));

        Assert.Equal(409, ex.StatusCode);
    }

    // ---------------- Resolución ----------------

    [Fact]
    public async Task ApproveAsync_SolicitudPendiente_QuedaAprobadaConAutor()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var created = await EmployeeService(db).CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));

        var approved = await AdminService(db).ApproveAsync(created.Id, new DecideAbsenceRequest("Adelante"));

        Assert.Equal(AbsenceStatus.Approved, approved.Status);
        Assert.NotNull(approved.DecidedAt);
        Assert.Equal("Adelante", approved.DecisionComment);
    }

    [Fact]
    public async Task ApproveAsync_SolicitudYaResuelta_LanzaConflicto()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var created = await EmployeeService(db).CreateAsync(
            new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));
        var admin = AdminService(db);
        await admin.ApproveAsync(created.Id, new DecideAbsenceRequest(null));

        var ex = await Assert.ThrowsAsync<AppException>(() => admin.ApproveAsync(created.Id, new DecideAbsenceRequest(null)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CancelAsync_SolicitudPropiaPendiente_QuedaRetirada()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var svc = EmployeeService(db);
        var created = await svc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));

        var cancelled = await svc.CancelAsync(created.Id);

        Assert.Equal(AbsenceStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelAsync_SolicitudYaAprobada_LanzaConflicto()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var empSvc = EmployeeService(db);
        var created = await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));
        await AdminService(db).ApproveAsync(created.Id, new DecideAbsenceRequest(null));

        var ex = await Assert.ThrowsAsync<AppException>(() => empSvc.CancelAsync(created.Id));

        Assert.Equal(409, ex.StatusCode);
    }

    // ---------------- Saldo ----------------

    [Fact]
    public async Task GetBalanceAsync_DescuentaAprobadasYPendientes()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db, days: 23m);
        var empSvc = EmployeeService(db);
        var first = await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));
        await AdminService(db).ApproveAsync(first.Id, new DecideAbsenceRequest(null));
        await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 4), null));

        var balance = await empSvc.GetBalanceAsync(1, 2026);

        Assert.Equal(23m, balance.AllowanceDays);
        Assert.Equal(5m, balance.ApprovedDays);
        Assert.Equal(3m, balance.PendingDays);
        Assert.Equal(15m, balance.AvailableDays);
    }

    [Fact]
    public async Task GetBalanceAsync_LasAusenciasQueNoConsumenSaldoNoDescuentan()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db, days: 10m);
        await EmployeeService(db).CreateAsync(
            new CreateAbsenceRequest(Enfermedad, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));

        var balance = await EmployeeService(db).GetBalanceAsync(1, 2026);

        Assert.Equal(10m, balance.AvailableDays);
    }

    [Fact]
    public async Task GetBalanceAsync_EmpleadoConsultandoOtro_Lanza403()
    {
        var db = SeedDb();

        var ex = await Assert.ThrowsAsync<AppException>(() => EmployeeService(db).GetBalanceAsync(2, 2026));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task SetAllowanceAsync_SobreEscribeLaAsignacionExistente()
    {
        var db = SeedDb();
        var admin = AdminService(db);
        await admin.SetAllowanceAsync(new SetVacationAllowanceRequest(1, 2026, 23m));

        var updated = await admin.SetAllowanceAsync(new SetVacationAllowanceRequest(1, 2026, 25m));

        Assert.Equal(25m, updated.AllowanceDays);
        Assert.Single(db.VacationAllowances);
    }

    // ---------------- Listado y calendario ----------------

    [Fact]
    public async Task GetPagedAsync_EmpleadoSoloVeSusSolicitudes()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        await EmployeeService(db).CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9), null));
        await AdminService(db).CreateAsync(new CreateAbsenceRequest(Enfermedad, new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 3), null, EmployeeId: 2));

        var mine = await EmployeeService(db).GetPagedAsync(new AbsenceQuery());

        Assert.All(mine.Items, a => Assert.Equal(1, a.EmployeeId));
        Assert.Single(mine.Items);
    }

    [Fact]
    public async Task GetPagedAsync_EmpleadoFiltrandoPorOtro_Lanza403()
    {
        var db = SeedDb();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            EmployeeService(db).GetPagedAsync(new AbsenceQuery(EmployeeId: 2)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task GetVacationCalendarAsync_AgrupaPorEmpleadoEIncluyePendientes()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        await EmployeeService(db).CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 14), null));

        var calendar = await AdminService(db).GetVacationCalendarAsync(2026);

        Assert.Equal(2026, calendar.Year);
        Assert.Equal(2, calendar.Employees.Count);          // los dos empleados activos
        var eva = calendar.Employees.First(e => e.EmployeeId == 1);
        Assert.Single(eva.Absences);
        Assert.Equal(AbsenceStatus.Pending, eva.Absences[0].Status);
    }

    [Fact]
    public async Task GetVacationCalendarAsync_NoIncluyeLasRetiradas()
    {
        var db = SeedDb();
        await GiveAllowanceAsync(db);
        var empSvc = EmployeeService(db);
        var created = await empSvc.CreateAsync(new CreateAbsenceRequest(Vacaciones, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 14), null));
        await empSvc.CancelAsync(created.Id);

        var calendar = await AdminService(db).GetVacationCalendarAsync(2026);

        Assert.Empty(calendar.Employees.First(e => e.EmployeeId == 1).Absences);
    }
}
