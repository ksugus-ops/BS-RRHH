using HRIA.Application.Auth;
using HRIA.Application.Auth.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Persistence;
using HRIA.Infrastructure.Security;
using HRIA.Tests.Common;

namespace HRIA.Tests.Auth;

public class PasswordChangeTests
{
    private const string OriginalPassword = "Demo1234!";
    private readonly Pbkdf2PasswordHasher _hasher = new();

    private sealed class StubJwt : IJwtTokenGenerator
    {
        public (string token, DateTime expiresAtUtc) Generate(User user)
            => ("test-token", DateTime.UtcNow.AddHours(1));
    }

    /// <summary>Empleado 1 con usuario 1 (empleado) y empleado 2 con usuario 2 (admin).</summary>
    private AppDbContext SeedDb()
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
            EmployeeId = 1,
            Email = "eva@hria.local",
            PasswordHash = _hasher.Hash(OriginalPassword),
            Role = Role.Employee,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Users.Add(new User
        {
            Id = 2,
            EmployeeId = 2,
            Email = "ana@hria.local",
            PasswordHash = _hasher.Hash(OriginalPassword),
            Role = Role.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.SaveChanges();
        return db;
    }

    private AuthService AsEmployee(AppDbContext db) =>
        new(db, _hasher, new StubJwt(), FakeCurrentUser.Employee(userId: 1, employeeId: 1));

    private AuthService AsAdmin(AppDbContext db) =>
        new(db, _hasher, new StubJwt(), FakeCurrentUser.Admin(userId: 2, employeeId: 2));

    private string HashOf(AppDbContext db, int userId) =>
        db.Users.First(u => u.Id == userId).PasswordHash;

    // ---------------- Cambio propio ----------------

    [Fact]
    public async Task ChangePassword_ConLaActualCorrecta_CambiaElHash()
    {
        var db = SeedDb();
        var antes = HashOf(db, 1);

        await AsEmployee(db).ChangePasswordAsync(new ChangePasswordRequest(OriginalPassword, "NuevaClave99!"));

        var despues = HashOf(db, 1);
        Assert.NotEqual(antes, despues);
        Assert.True(_hasher.Verify("NuevaClave99!", despues));
        Assert.False(_hasher.Verify(OriginalPassword, despues));
    }

    [Fact]
    public async Task ChangePassword_ConLaActualIncorrecta_NoCambiaNada()
    {
        var db = SeedDb();
        var antes = HashOf(db, 1);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            AsEmployee(db).ChangePasswordAsync(new ChangePasswordRequest("EquivocadaXX", "NuevaClave99!")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(antes, HashOf(db, 1));
    }

    [Fact]
    public async Task ChangePassword_MismaQueLaActual_SeRechaza()
    {
        var db = SeedDb();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            AsEmployee(db).ChangePasswordAsync(new ChangePasswordRequest(OriginalPassword, OriginalPassword)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_SoloAfectaAlUsuarioAutenticado()
    {
        var db = SeedDb();
        var otroAntes = HashOf(db, 2);

        await AsEmployee(db).ChangePasswordAsync(new ChangePasswordRequest(OriginalPassword, "NuevaClave99!"));

        Assert.Equal(otroAntes, HashOf(db, 2));
    }

    [Fact]
    public async Task ChangePassword_QuedaRegistradoEnAuditoriaSinLaContrasena()
    {
        var db = SeedDb();

        await AsEmployee(db).ChangePasswordAsync(new ChangePasswordRequest(OriginalPassword, "NuevaClave99!"));

        var log = db.AuditLogs.Single(a => a.Action == "ChangePassword");
        Assert.Equal(1, log.UserId);
        Assert.DoesNotContain("NuevaClave99!", log.Details);
        Assert.DoesNotContain(OriginalPassword, log.Details);
    }

    // ---------------- Restablecimiento por administrador ----------------

    [Fact]
    public async Task ResetPassword_SinIndicarNinguna_GeneraUnaValidaYLaDevuelveUnaVez()
    {
        var db = SeedDb();

        var r = await AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest());

        Assert.Equal("eva@hria.local", r.Email);
        Assert.True(r.TemporaryPassword.Length >= AuthService.MinPasswordLength);
        // La devuelta es la que queda vigente.
        Assert.True(_hasher.Verify(r.TemporaryPassword, HashOf(db, 1)));
    }

    [Fact]
    public async Task ResetPassword_DosVeces_GeneraContrasenasDistintas()
    {
        var db = SeedDb();
        var admin = AsAdmin(db);

        var a = await admin.ResetPasswordAsync(1, new ResetPasswordRequest());
        var b = await admin.ResetPasswordAsync(1, new ResetPasswordRequest());

        Assert.NotEqual(a.TemporaryPassword, b.TemporaryPassword);
    }

    [Fact]
    public async Task ResetPassword_ConUnaIndicada_LaAplica()
    {
        var db = SeedDb();

        var r = await AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest("ElegidaPorRRHH1"));

        Assert.Equal("ElegidaPorRRHH1", r.TemporaryPassword);
        Assert.True(_hasher.Verify("ElegidaPorRRHH1", HashOf(db, 1)));
    }

    [Fact]
    public async Task ResetPassword_DemasiadoCorta_SeRechaza()
    {
        var db = SeedDb();
        var antes = HashOf(db, 1);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest("corta")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(antes, HashOf(db, 1));
    }

    [Fact]
    public async Task ResetPassword_EmpleadoInexistente_Lanza404()
    {
        var db = SeedDb();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            AsAdmin(db).ResetPasswordAsync(999, new ResetPasswordRequest()));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_RegistraQuienLoHizoYSobreQuien()
    {
        var db = SeedDb();

        await AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest());

        var log = db.AuditLogs.Single(a => a.Action == "ResetPassword");
        Assert.Equal(2, log.UserId);              // lo hizo la administradora
        Assert.Equal("1", log.EntityId);          // sobre el usuario de Eva
        Assert.Contains("eva@hria.local", log.Details);
    }

    [Fact]
    public async Task ResetPassword_LaContrasenaAnteriorDejaDeValer()
    {
        var db = SeedDb();

        await AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest());

        Assert.False(_hasher.Verify(OriginalPassword, HashOf(db, 1)));
    }

    [Fact]
    public async Task LaContrasenaNuncaSeGuardaEnClaro()
    {
        var db = SeedDb();

        var r = await AsAdmin(db).ResetPasswordAsync(1, new ResetPasswordRequest());

        // Ni en el usuario ni en la auditoría debe aparecer el texto.
        Assert.DoesNotContain(r.TemporaryPassword, HashOf(db, 1));
        Assert.All(db.AuditLogs, a => Assert.DoesNotContain(r.TemporaryPassword, a.Details ?? string.Empty));
    }
}
