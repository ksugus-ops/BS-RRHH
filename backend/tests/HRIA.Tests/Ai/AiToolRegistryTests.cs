using System.Text.Json;
using FluentAssertions;
using HRIA.Application.Ai;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Tests.Common;
using Xunit;

namespace HRIA.Tests.Ai;

public class AiToolRegistryTests
{
    private static Employee SeedEmp(AppDbContext db, Department dept, string first)
    {
        var e = new Employee
        {
            FirstName = first,
            LastName = "Test",
            Email = $"{first}@hria.local".ToLower(),
            Department = dept,
            Position = "Dev",
            HireDate = new DateOnly(2022, 1, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(e);
        db.SaveChanges();
        return e;
    }

    private static void AddCompletedToday(AppDbContext db, int empId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Workdays.Add(new Workday
        {
            EmployeeId = empId,
            Date = today,
            CheckIn = today.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            CheckOut = today.ToDateTime(new TimeOnly(17, 0), DateTimeKind.Utc),
            Status = WorkdayStatus.Completed
        });
        db.SaveChanges();
    }

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void BuildTools_Admin_IncludesAllTools()
    {
        using var db = TestDb.Create();
        var tools = new AiToolRegistry(db).BuildTools(Role.Admin, 1);
        var names = tools.Select(t => t.Name).ToList();

        names.Should().Contain(new[]
        {
            "get_current_working_employees", "get_open_time_entries",
            "get_incomplete_workdays", "get_department_hours_summary", "get_employee_hours_summary"
        });
    }

    [Fact]
    public void BuildTools_Employee_OnlyExposesOwnHoursSummary()
    {
        using var db = TestDb.Create();
        var tools = new AiToolRegistry(db).BuildTools(Role.Employee, 1);
        var names = tools.Select(t => t.Name).ToList();

        names.Should().ContainSingle().Which.Should().Be("get_employee_hours_summary");
        // Las herramientas de administrador NO están disponibles para el empleado.
        names.Should().NotContain("get_current_working_employees");
        names.Should().NotContain("get_department_hours_summary");
    }

    [Fact]
    public async Task EmployeeHoursSummary_AsEmployee_IgnoresRequestedOtherEmployeeId()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Desarrollo" };
        db.Departments.Add(dept);
        var me = SeedEmp(db, dept, "Eva");
        var other = SeedEmp(db, dept, "Carlos");
        AddCompletedToday(db, me.Id);
        AddCompletedToday(db, other.Id);

        var tool = new AiToolRegistry(db).BuildTools(Role.Employee, me.Id)
            .Single(t => t.Name == "get_employee_hours_summary");

        // Intenta consultar los datos de OTRO empleado -> debe ignorarse y usar los propios.
        var result = await tool.ExecuteAsync(Args($"{{\"employeeId\": {other.Id}}}"), default);

        result.HumanSummary.Should().Contain("Eva");
        result.HumanSummary.Should().NotContain("Carlos");
    }

    [Fact]
    public async Task CurrentWorkingEmployees_ReturnsEmployeesWithOpenWorkday()
    {
        using var db = TestDb.Create();
        var dept = new Department { Name = "Ventas" };
        db.Departments.Add(dept);
        var emp = SeedEmp(db, dept, "Luis");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Workdays.Add(new Workday { EmployeeId = emp.Id, Date = today, CheckIn = DateTime.UtcNow.AddHours(-1), Status = WorkdayStatus.Open });
        db.SaveChanges();

        var tool = new AiToolRegistry(db).BuildTools(Role.Admin, 1)
            .Single(t => t.Name == "get_current_working_employees");
        var result = await tool.ExecuteAsync(null, default);

        result.HumanSummary.Should().Contain("Luis");
    }

    [Fact]
    public async Task EmployeeHoursSummary_InvalidEmployee_ReturnsControlledMessage()
    {
        using var db = TestDb.Create();
        // Admin pide un empleado inexistente.
        var tool = new AiToolRegistry(db).BuildTools(Role.Admin, 1)
            .Single(t => t.Name == "get_employee_hours_summary");

        var result = await tool.ExecuteAsync(Args("{\"employeeId\": 9999}"), default);

        result.HumanSummary.Should().Contain("No se encontró");
    }
}
