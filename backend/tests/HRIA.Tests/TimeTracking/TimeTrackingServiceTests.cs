using FluentAssertions;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Schedules;
using HRIA.Application.TimeTracking;
using HRIA.Application.TimeTracking.Dtos;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;
using Xunit;

namespace HRIA.Tests.TimeTracking;

public class TimeTrackingServiceTests
{
    private const int EmpId = 5;

    private static (TimeTrackingService svc, AppDbContext db) Build(int employeeId = EmpId)
    {
        var db = TestDb.Create();
        var svc = new TimeTrackingService(
            db,
            FakeCurrentUser.Employee(userId: 50, employeeId: employeeId),
            new ExpectedMinutesCalculator(db));
        return (svc, db);
    }

    // --- Reglas de negocio ---

    [Fact]
    public async Task CheckIn_CreatesOpenWorkday()
    {
        var (svc, db) = Build();
        var status = await svc.CheckInAsync();

        status.State.Should().Be(TimeState.Working);
        db.Workdays.Should().ContainSingle(w => w.Status == WorkdayStatus.Open && w.CheckOut == null);
    }

    [Fact]
    public async Task CheckIn_Twice_ThrowsConflict_BR01()
    {
        var (svc, _) = Build();
        await svc.CheckInAsync();

        var act = () => svc.CheckInAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task StartBreak_WithoutWorkday_ThrowsConflict_BR02()
    {
        var (svc, _) = Build();
        var act = () => svc.StartBreakAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task StartBreak_Twice_ThrowsConflict_BR03()
    {
        var (svc, _) = Build();
        await svc.CheckInAsync();
        await svc.StartBreakAsync();

        var act = () => svc.StartBreakAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task EndBreak_WithoutOpenBreak_ThrowsConflict_BR04()
    {
        var (svc, _) = Build();
        await svc.CheckInAsync();

        var act = () => svc.EndBreakAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CheckOut_WithOpenBreak_ThrowsConflict_BR05()
    {
        var (svc, _) = Build();
        await svc.CheckInAsync();
        await svc.StartBreakAsync();

        var act = () => svc.CheckOutAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task CheckOut_WithoutWorkday_ThrowsConflict_BR06()
    {
        var (svc, _) = Build();
        var act = () => svc.CheckOutAsync();
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    // --- Flujo completo y estados ---

    [Fact]
    public async Task FullFlow_CheckIn_Break_EndBreak_CheckOut_CompletesWorkday()
    {
        var (svc, db) = Build();

        (await svc.CheckInAsync()).State.Should().Be(TimeState.Working);
        (await svc.StartBreakAsync()).State.Should().Be(TimeState.OnBreak);
        (await svc.EndBreakAsync()).State.Should().Be(TimeState.Working);

        var final = await svc.CheckOutAsync();
        final.Status.Should().Be(WorkdayStatus.Completed);
        final.CheckOut.Should().NotBeNull();

        var status = await svc.GetStatusAsync();
        status.State.Should().Be(TimeState.NotStarted);
    }

    [Fact]
    public async Task GetStatus_ReflectsOnBreak()
    {
        var (svc, _) = Build();
        await svc.CheckInAsync();
        await svc.StartBreakAsync();

        (await svc.GetStatusAsync()).State.Should().Be(TimeState.OnBreak);
    }

    [Fact]
    public async Task CheckIn_AfterCheckOut_AllowsNewWorkday()
    {
        var (svc, db) = Build();
        await svc.CheckInAsync();
        await svc.CheckOutAsync();

        // Tras cerrar la jornada se puede fichar de nuevo (misma jornada del día).
        var act = () => svc.CheckInAsync();
        await act.Should().NotThrowAsync();
    }

    // --- BR-08: jornada incompleta ---

    [Fact]
    public async Task GetStatus_MarksStaleOpenWorkdayAsIncomplete_BR08()
    {
        var (svc, db) = Build();
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        db.Workdays.Add(new Workday
        {
            EmployeeId = EmpId,
            Date = twoDaysAgo,
            CheckIn = twoDaysAgo.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            CheckOut = null,
            Status = WorkdayStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var status = await svc.GetStatusAsync();

        status.State.Should().Be(TimeState.NotStarted);
        db.Workdays.Single().Status.Should().Be(WorkdayStatus.Incomplete);
    }

    // --- Protección horizontal ---

    [Fact]
    public async Task GetWorkdays_AsEmployee_ReturnsOnlyOwn()
    {
        var (svc, db) = Build(employeeId: EmpId);
        var now = DateTime.UtcNow;
        db.Workdays.Add(new Workday { EmployeeId = EmpId, Date = DateOnly.FromDateTime(now), CheckIn = now, CheckOut = now, Status = WorkdayStatus.Completed });
        db.Workdays.Add(new Workday { EmployeeId = 999, Date = DateOnly.FromDateTime(now), CheckIn = now, CheckOut = now, Status = WorkdayStatus.Completed });
        await db.SaveChangesAsync();

        // Aunque pida el employeeId de otro, como empleado solo ve el suyo.
        var result = await svc.GetWorkdaysAsync(new WorkdayQuery(EmployeeId: 999));

        result.Should().OnlyContain(w => w.EmployeeId == EmpId);
    }
}
