using FluentAssertions;
using HRIA.Application.Auth;
using HRIA.Application.Auth.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Infrastructure.Security;
using HRIA.Tests.Common;
using Xunit;

namespace HRIA.Tests.Auth;

public class AuthServiceTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    // Generador de tokens de prueba (no valida firma; solo devuelve un valor).
    private sealed class StubJwt : IJwtTokenGenerator
    {
        public (string token, DateTime expiresAtUtc) Generate(User user)
            => ("test-token", DateTime.UtcNow.AddHours(1));
    }

    private AuthService BuildService(AppDbContext db, FakeCurrentUser? user = null) =>
        new(db, _hasher, new StubJwt(), user ?? FakeCurrentUser.Admin());

    private User SeedUser(AppDbContext db, string email = "admin@hria.local",
        string password = "Demo1234!", bool userActive = true, bool employeeActive = true)
    {
        var dept = new Department { Name = "Recursos Humanos" };
        var emp = new Employee
        {
            FirstName = "Ana",
            LastName = "Admin",
            Email = email,
            Department = dept,
            Position = "Responsable",
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = employeeActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Employee = emp,
            Email = email,
            PasswordHash = _hasher.Hash(password),
            Role = Role.Admin,
            IsActive = userActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        using var db = TestDb.Create();
        SeedUser(db);
        var service = BuildService(db);

        var result = await service.LoginAsync(new LoginRequest("admin@hria.local", "Demo1234!"));

        result.Token.Should().Be("test-token");
        result.User.Email.Should().Be("admin@hria.local");
        result.User.Role.Should().Be(Role.Admin);
        // Se registra la auditoría del login.
        db.AuditLogs.Should().ContainSingle(a => a.Action == "Login");
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveOnEmail()
    {
        using var db = TestDb.Create();
        SeedUser(db);
        var service = BuildService(db);

        var result = await service.LoginAsync(new LoginRequest("ADMIN@HRIA.LOCAL", "Demo1234!"));

        result.User.Email.Should().Be("admin@hria.local");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        using var db = TestDb.Create();
        SeedUser(db);
        var service = BuildService(db);

        var act = () => service.LoginAsync(new LoginRequest("admin@hria.local", "incorrecta"));

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorized()
    {
        using var db = TestDb.Create();
        var service = BuildService(db);

        var act = () => service.LoginAsync(new LoginRequest("nadie@hria.local", "Demo1234!"));

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsUnauthorized()
    {
        using var db = TestDb.Create();
        SeedUser(db, userActive: false);
        var service = BuildService(db);

        var act = () => service.LoginAsync(new LoginRequest("admin@hria.local", "Demo1234!"));

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithInactiveEmployee_ThrowsUnauthorized()
    {
        using var db = TestDb.Create();
        SeedUser(db, employeeActive: false);
        var service = BuildService(db);

        var act = () => service.LoginAsync(new LoginRequest("admin@hria.local", "Demo1234!"));

        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsDto()
    {
        using var db = TestDb.Create();
        var user = SeedUser(db);
        var service = BuildService(db);

        var dto = await service.GetCurrentUserAsync(user.Id);

        dto.Email.Should().Be("admin@hria.local");
        dto.Department.Should().Be("Recursos Humanos");
        dto.FullName.Should().Be("Ana Admin");
    }
}
