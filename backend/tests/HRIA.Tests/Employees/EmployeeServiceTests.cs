using FluentAssertions;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Employees;
using HRIA.Application.Employees.Dtos;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Infrastructure.Security;
using HRIA.Tests.Common;
using Xunit;

namespace HRIA.Tests.Employees;

public class EmployeeServiceTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    private static Department SeedDept(AppDbContext db, string name = "Desarrollo")
    {
        var d = new Department { Name = name };
        db.Departments.Add(d);
        db.SaveChanges();
        return d;
    }

    private Employee SeedEmployee(AppDbContext db, int deptId, string first, string last, string email,
        Role role = Role.Employee, bool active = true)
    {
        var now = DateTime.UtcNow;
        var emp = new Employee
        {
            FirstName = first,
            LastName = last,
            Email = email,
            DepartmentId = deptId,
            Position = "Dev",
            HireDate = new DateOnly(2022, 1, 1),
            IsActive = active,
            CreatedAt = now,
            UpdatedAt = now,
            User = new User { Email = email, PasswordHash = _hasher.Hash("Demo1234!"), Role = role, IsActive = active, CreatedAt = now, UpdatedAt = now }
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    private EmployeeService Service(AppDbContext db, HRIA.Application.Common.Interfaces.ICurrentUser user)
        => new(db, _hasher, user);

    [Fact]
    public async Task Create_ValidEmployee_CreatesEmployeeAndUser()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        var service = Service(db, FakeCurrentUser.Admin());

        var req = new CreateEmployeeRequest("Nuevo", "Empleado", "Nuevo.Emp@hria.local",
            dept.Id, "Analista", new DateOnly(2024, 5, 1), Role.Employee, "Secret123");

        var created = await service.CreateAsync(req);

        created.Id.Should().BeGreaterThan(0);
        created.Email.Should().Be("nuevo.emp@hria.local"); // normalizado a minúsculas
        db.Users.Should().ContainSingle(u => u.Email == "nuevo.emp@hria.local");
        db.AuditLogs.Should().ContainSingle(a => a.Action == "CreateEmployee");
    }

    [Fact]
    public async Task Create_DuplicateEmail_ThrowsConflict()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        SeedEmployee(db, dept.Id, "Ya", "Existe", "dup@hria.local");
        var service = Service(db, FakeCurrentUser.Admin());

        var req = new CreateEmployeeRequest("Otro", "Distinto", "DUP@hria.local",
            dept.Id, "Dev", new DateOnly(2024, 1, 1), Role.Employee, "Secret123");

        var act = () => service.CreateAsync(req);
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Create_NonexistentDepartment_ThrowsBadRequest()
    {
        using var db = TestDb.Create();
        var service = Service(db, FakeCurrentUser.Admin());

        var req = new CreateEmployeeRequest("Sin", "Depto", "sindepto@hria.local",
            999, "Dev", new DateOnly(2024, 1, 1), Role.Employee, "Secret123");

        var act = () => service.CreateAsync(req);
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_ChangesData()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        var emp = SeedEmployee(db, dept.Id, "Ana", "Perez", "ana@hria.local");
        var service = Service(db, FakeCurrentUser.Admin());

        var req = new UpdateEmployeeRequest("Ana María", "Perez", "ana@hria.local",
            dept.Id, "Tech Lead", new DateOnly(2021, 3, 1), Role.Admin);

        var updated = await service.UpdateAsync(emp.Id, req);

        updated.FirstName.Should().Be("Ana María");
        updated.Position.Should().Be("Tech Lead");
        updated.Role.Should().Be(Role.Admin);
        db.AuditLogs.Should().ContainSingle(a => a.Action == "UpdateEmployee");
    }

    [Fact]
    public async Task Update_DuplicateEmail_ThrowsConflict()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        SeedEmployee(db, dept.Id, "Uno", "Uno", "uno@hria.local");
        var emp2 = SeedEmployee(db, dept.Id, "Dos", "Dos", "dos@hria.local");
        var service = Service(db, FakeCurrentUser.Admin());

        var req = new UpdateEmployeeRequest("Dos", "Dos", "uno@hria.local",
            dept.Id, "Dev", new DateOnly(2022, 1, 1), Role.Employee);

        var act = () => service.UpdateAsync(emp2.Id, req);
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Deactivate_SetsInactiveOnEmployeeAndUser()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        var emp = SeedEmployee(db, dept.Id, "Baja", "Logica", "baja@hria.local");
        var service = Service(db, FakeCurrentUser.Admin());

        await service.DeactivateAsync(emp.Id);

        var reloaded = db.Employees.Find(emp.Id)!;
        reloaded.IsActive.Should().BeFalse();
        db.Users.Single(u => u.EmployeeId == emp.Id).IsActive.Should().BeFalse();
        db.AuditLogs.Should().ContainSingle(a => a.Action == "DeactivateEmployee");
    }

    [Fact]
    public async Task GetById_AsEmployee_ForOtherEmployee_ThrowsForbidden()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        var other = SeedEmployee(db, dept.Id, "Otra", "Persona", "otra@hria.local");
        // Empleado autenticado con employeeId distinto.
        var service = Service(db, FakeCurrentUser.Employee(userId: 50, employeeId: 999));

        var act = () => service.GetByIdAsync(other.Id);
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetById_AsEmployee_ForSelf_Returns()
    {
        using var db = TestDb.Create();
        var dept = SeedDept(db);
        var me = SeedEmployee(db, dept.Id, "Yo", "Mismo", "yo@hria.local");
        var service = Service(db, FakeCurrentUser.Employee(userId: 50, employeeId: me.Id));

        var dto = await service.GetByIdAsync(me.Id);
        dto.Email.Should().Be("yo@hria.local");
    }

    [Fact]
    public async Task GetPaged_FiltersBySearchAndDepartment_AndPaginates()
    {
        using var db = TestDb.Create();
        var dev = SeedDept(db, "Desarrollo");
        var ventas = SeedDept(db, "Ventas");
        SeedEmployee(db, dev.Id, "Carlos", "Gomez", "carlos@hria.local");
        SeedEmployee(db, dev.Id, "Marta", "Ruiz", "marta@hria.local");
        SeedEmployee(db, ventas.Id, "Luis", "Perez", "luis@hria.local");
        var service = Service(db, FakeCurrentUser.Admin());

        // Filtro por departamento.
        var byDept = await service.GetPagedAsync(new EmployeeQuery(DepartmentId: dev.Id));
        byDept.Total.Should().Be(2);

        // Búsqueda case-insensitive.
        var bySearch = await service.GetPagedAsync(new EmployeeQuery(Search: "MART"));
        bySearch.Total.Should().Be(1);
        bySearch.Items[0].FullName.Should().Be("Marta Ruiz");

        // Paginación.
        var page1 = await service.GetPagedAsync(new EmployeeQuery(Page: 1, PageSize: 2));
        page1.Items.Should().HaveCount(2);
        page1.Total.Should().Be(3);
        page1.TotalPages.Should().Be(2);
    }
}
