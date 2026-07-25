using HRIA.Application.Common.Exceptions;
using HRIA.Application.Schedules;
using HRIA.Application.Schedules.Dtos;
using HRIA.Domain.Entities;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;

namespace HRIA.Tests.Schedules;

public class ScheduleServiceTests
{
    private static (ScheduleService svc, AppDbContext db) CreateService(FakeCurrentUser? user = null)
    {
        var db = TestDb.Create();
        SeedEmployees(db);
        return (new ScheduleService(db, user ?? FakeCurrentUser.Admin()), db);
    }

    private static void SeedEmployees(AppDbContext db)
    {
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
            FirstName = "Baja",
            LastName = "Empleado",
            Email = "baja@hria.local",
            DepartmentId = 1,
            Position = "Dev",
            HireDate = new DateOnly(2024, 1, 1),
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
    }

    private static CreateScheduleRequest OfficeSchedule(string name = "Oficina") => new(
        name,
        "Lunes a viernes",
        new List<ScheduleSlotInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            new(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        });

    // ---------------- Plantillas ----------------

    [Fact]
    public async Task CreateAsync_HorarioValido_CalculaMinutosSemanales()
    {
        var (svc, _) = CreateService();

        var created = await svc.CreateAsync(OfficeSchedule());

        Assert.Equal("Oficina", created.Name);
        Assert.Equal(2, created.Slots.Count);
        Assert.Equal(16 * 60, created.WeeklyMinutes);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_LanzaConflicto()
    {
        var (svc, _) = CreateService();
        await svc.CreateAsync(OfficeSchedule());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(OfficeSchedule()));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_SinTramos_LanzaBadRequest()
    {
        var (svc, _) = CreateService();
        var request = new CreateScheduleRequest("Vacío", null, new List<ScheduleSlotInput>());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(request));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_TramoQueTerminaAntesDeEmpezar_LanzaBadRequest()
    {
        var (svc, _) = CreateService();
        var request = new CreateScheduleRequest("Invertido", null, new List<ScheduleSlotInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0)),
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(request));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_TramosSolapadosElMismoDia_LanzaBadRequest()
    {
        var (svc, _) = CreateService();
        var request = new CreateScheduleRequest("Solapado", null, new List<ScheduleSlotInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(14, 0)),
            new(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(18, 0)),
        });

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(request));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("solapan", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_TramosContiguosElMismoDia_SeAceptan()
    {
        var (svc, _) = CreateService();
        var request = new CreateScheduleRequest("Partido", null, new List<ScheduleSlotInput>
        {
            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(14, 0)),
            new(DayOfWeek.Monday, new TimeOnly(15, 0), new TimeOnly(18, 0)),
        });

        var created = await svc.CreateAsync(request);

        Assert.Equal(8 * 60, created.WeeklyMinutes);
    }

    [Fact]
    public async Task UpdateAsync_ReemplazaLosTramosEnBloque()
    {
        var (svc, _) = CreateService();
        var created = await svc.CreateAsync(OfficeSchedule());

        var updated = await svc.UpdateAsync(created.Id, new UpdateScheduleRequest(
            "Oficina", "Solo lunes", true,
            new List<ScheduleSlotInput> { new(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(15, 0)) }));

        Assert.Single(updated.Slots);
        Assert.Equal(7 * 60, updated.WeeklyMinutes);
    }

    [Fact]
    public async Task DeactivateAsync_ConAsignacionVigente_LanzaConflicto()
    {
        var (svc, _) = CreateService();
        var schedule = await svc.CreateAsync(OfficeSchedule());
        await svc.AssignAsync(new CreateScheduleAssignmentRequest(
            schedule.Id, 1, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10), null));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.DeactivateAsync(schedule.Id));

        Assert.Equal(409, ex.StatusCode);
    }

    // ---------------- Asignaciones ----------------

    [Fact]
    public async Task AssignAsync_PeriodosSolapados_LanzaConflicto()
    {
        var (svc, _) = CreateService();
        var a = await svc.CreateAsync(OfficeSchedule("Horario A"));
        var b = await svc.CreateAsync(OfficeSchedule("Horario B"));

        await svc.AssignAsync(new CreateScheduleAssignmentRequest(
            a.Id, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AssignAsync(
            new CreateScheduleAssignmentRequest(b.Id, 1, new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31))));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_PeriodosConsecutivosSinSolape_SeAceptan()
    {
        var (svc, _) = CreateService();
        var a = await svc.CreateAsync(OfficeSchedule("Horario A"));
        var b = await svc.CreateAsync(OfficeSchedule("Horario B"));

        await svc.AssignAsync(new CreateScheduleAssignmentRequest(
            a.Id, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));
        var second = await svc.AssignAsync(new CreateScheduleAssignmentRequest(
            b.Id, 1, new DateOnly(2026, 7, 1), null));

        Assert.Equal(new DateOnly(2026, 7, 1), second.StartDate);
    }

    [Fact]
    public async Task AssignAsync_AsignacionAbiertaBloqueaCualquierPosterior()
    {
        var (svc, _) = CreateService();
        var a = await svc.CreateAsync(OfficeSchedule("Horario A"));
        var b = await svc.CreateAsync(OfficeSchedule("Horario B"));

        await svc.AssignAsync(new CreateScheduleAssignmentRequest(a.Id, 1, new DateOnly(2026, 1, 1), null));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AssignAsync(
            new CreateScheduleAssignmentRequest(b.Id, 1, new DateOnly(2027, 1, 1), null)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_EmpleadoDadoDeBaja_LanzaBadRequest()
    {
        var (svc, _) = CreateService();
        var schedule = await svc.CreateAsync(OfficeSchedule());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AssignAsync(
            new CreateScheduleAssignmentRequest(schedule.Id, 2, new DateOnly(2026, 1, 1), null)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_FechaFinAnteriorAInicio_LanzaBadRequest()
    {
        var (svc, _) = CreateService();
        var schedule = await svc.CreateAsync(OfficeSchedule());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AssignAsync(
            new CreateScheduleAssignmentRequest(schedule.Id, 1, new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1))));

        Assert.Equal(400, ex.StatusCode);
    }

    // ---------------- Horario vigente ----------------

    [Fact]
    public async Task GetEffectiveScheduleAsync_DevuelveElVigenteEnLaFecha()
    {
        var (svc, _) = CreateService();
        var a = await svc.CreateAsync(OfficeSchedule("Horario A"));
        var b = await svc.CreateAsync(OfficeSchedule("Horario B"));
        await svc.AssignAsync(new CreateScheduleAssignmentRequest(a.Id, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));
        await svc.AssignAsync(new CreateScheduleAssignmentRequest(b.Id, 1, new DateOnly(2026, 7, 1), null));

        var enMarzo = await svc.GetEffectiveScheduleAsync(1, new DateOnly(2026, 3, 15));
        var enAgosto = await svc.GetEffectiveScheduleAsync(1, new DateOnly(2026, 8, 15));

        Assert.Equal("Horario A", enMarzo!.Name);
        Assert.Equal("Horario B", enAgosto!.Name);
    }

    [Fact]
    public async Task GetEffectiveScheduleAsync_SinAsignacion_DevuelveNull()
    {
        var (svc, _) = CreateService();

        var result = await svc.GetEffectiveScheduleAsync(1, new DateOnly(2026, 3, 15));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEffectiveScheduleAsync_EmpleadoConsultandoOtroEmpleado_Lanza403()
    {
        var (svc, _) = CreateService(FakeCurrentUser.Employee(userId: 5, employeeId: 1));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.GetEffectiveScheduleAsync(2, new DateOnly(2026, 3, 15)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task GetAssignmentsAsync_EmpleadoSoloVeLasSuyas()
    {
        var (adminSvc, db) = CreateService();
        var schedule = await adminSvc.CreateAsync(OfficeSchedule());
        await adminSvc.AssignAsync(new CreateScheduleAssignmentRequest(schedule.Id, 1, new DateOnly(2026, 1, 1), null));

        var employeeSvc = new ScheduleService(db, FakeCurrentUser.Employee(userId: 5, employeeId: 1));
        var mine = await employeeSvc.GetAssignmentsAsync(null, null);

        Assert.Single(mine);
        Assert.Equal(1, mine[0].EmployeeId);
    }

    [Fact]
    public async Task GetAssignmentsAsync_EmpleadoPidiendoOtroEmpleado_Lanza403()
    {
        var (_, db) = CreateService();
        var employeeSvc = new ScheduleService(db, FakeCurrentUser.Employee(userId: 5, employeeId: 1));

        var ex = await Assert.ThrowsAsync<AppException>(() => employeeSvc.GetAssignmentsAsync(2, null));

        Assert.Equal(403, ex.StatusCode);
    }
}
