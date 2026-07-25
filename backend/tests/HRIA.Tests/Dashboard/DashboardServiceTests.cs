using FluentAssertions;
using HRIA.Application.Absences;
using HRIA.Application.Dashboard;
using HRIA.Application.Schedules;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;
using Xunit;

namespace HRIA.Tests.Dashboard;

public class DashboardServiceTests
{
    private static Employee Emp(AppDbContext db, Department dept, string first, bool active = true)
    {
        var e = new Employee
        {
            FirstName = first,
            LastName = "Test",
            Email = $"{first}@hria.local".ToLower(),
            Department = dept,
            Position = "Dev",
            HireDate = new DateOnly(2022, 1, 1),
            IsActive = active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(e);
        return e;
    }

    [Fact]
    public async Task GetSummary_ComputesIndicators()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Desarrollo" };
        db.Departments.Add(dept);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var working = Emp(db, dept, "Working");
        var onBreak = Emp(db, dept, "OnBreak");
        var incomplete = Emp(db, dept, "Incomplete");
        Emp(db, dept, "Inactive", active: false);
        db.SaveChanges();

        // Trabajando ahora (jornada abierta sin descanso).
        db.Workdays.Add(new Workday { EmployeeId = working.Id, Date = today, CheckIn = now.AddHours(-2), Status = WorkdayStatus.Open });
        // En descanso (jornada abierta con descanso abierto).
        var b = new Workday { EmployeeId = onBreak.Id, Date = today, CheckIn = now.AddHours(-3), Status = WorkdayStatus.Open };
        b.Breaks.Add(new Break { StartTime = now.AddMinutes(-10), EndTime = null });
        db.Workdays.Add(b);
        // Incompleta.
        db.Workdays.Add(new Workday { EmployeeId = incomplete.Id, Date = today.AddDays(-1), CheckIn = now.AddDays(-1), Status = WorkdayStatus.Incomplete });
        db.SaveChanges();

        var svc = new DashboardService(db, new ExpectedMinutesCalculator(db), new WorkingDayCalculator(db));
        var summary = await svc.GetSummaryAsync();

        summary.ActiveEmployees.Should().Be(3); // 3 activos de 4
        summary.Working.Should().Be(1);
        summary.OnBreak.Should().Be(1);
        summary.IncompleteWorkdays.Should().Be(1);
        summary.HoursTodayMinutes.Should().BeGreaterThan(0);
        summary.RecentPunches.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSummary_StaleOpenWorkday_CountsAsIncomplete()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Ventas" };
        db.Departments.Add(dept);
        var emp = Emp(db, dept, "Stale");
        db.SaveChanges();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Jornada abierta de un día previo (olvido de salida) => se cuenta como incompleta.
        db.Workdays.Add(new Workday
        {
            EmployeeId = emp.Id,
            Date = today.AddDays(-1),
            CheckIn = DateTime.UtcNow.AddDays(-1),
            CheckOut = null,
            Status = WorkdayStatus.Open
        });
        db.SaveChanges();

        var summary = await new DashboardService(db, new ExpectedMinutesCalculator(db), new WorkingDayCalculator(db)).GetSummaryAsync();

        summary.Working.Should().Be(0);
        summary.IncompleteWorkdays.Should().Be(1);
    }

    [Fact]
    public async Task GetHoursByDay_ReturnsContinuousSeriesWithSums()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Ops" };
        db.Departments.Add(dept);
        var emp = Emp(db, dept, "Hours");
        db.SaveChanges();

        var day = new DateOnly(2026, 3, 10);
        var wd = new Workday
        {
            EmployeeId = emp.Id,
            Date = day,
            CheckIn = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
            CheckOut = day.ToDateTime(new TimeOnly(16, 0), DateTimeKind.Utc),
            Status = WorkdayStatus.Completed
        };
        wd.Breaks.Add(new Break
        {
            StartTime = day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
            EndTime = day.ToDateTime(new TimeOnly(12, 30), DateTimeKind.Utc)
        });
        db.Workdays.Add(wd);
        db.SaveChanges();

        var series = await new DashboardService(db, new ExpectedMinutesCalculator(db), new WorkingDayCalculator(db)).GetHoursByDayAsync(day.AddDays(-1), day.AddDays(1));

        series.Should().HaveCount(3); // día-1, día, día+1 (serie continua)
        series.Single(p => p.Date == day).Hours.Should().Be(7.5); // 8h - 30min
        series.Single(p => p.Date == day.AddDays(-1)).Hours.Should().Be(0); // día vacío
    }

    [Fact]
    public async Task GetHoursByDay_StaleIncompleteWorkday_DoesNotInflateHours()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Ops" };
        db.Departments.Add(dept);
        var emp = Emp(db, dept, "Olvido");
        db.SaveChanges();

        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        // Jornada sin salida de hace 3 días (olvido): no debe contar decenas de horas.
        db.Workdays.Add(new Workday
        {
            EmployeeId = emp.Id,
            Date = day,
            CheckIn = day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            CheckOut = null,
            Status = WorkdayStatus.Incomplete
        });
        db.SaveChanges();

        var series = await new DashboardService(db, new ExpectedMinutesCalculator(db), new WorkingDayCalculator(db)).GetHoursByDayAsync(day, day);

        series.Single().Hours.Should().Be(0);
    }
}
