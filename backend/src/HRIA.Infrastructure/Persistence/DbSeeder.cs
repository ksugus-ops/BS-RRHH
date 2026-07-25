using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Infrastructure.Persistence;

/// <summary>
/// Datos ficticios de demostración. Idempotente: solo siembra si la BD está vacía.
/// Genera departamentos, 10 empleados, usuarios demo y jornadas en distintos estados
/// (completas, incompletas, trabajando ahora y en descanso).
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo1234!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return; // ya sembrado

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // --- Departamentos ---
        var desarrollo = new Department { Name = "Desarrollo" };
        var rrhh = new Department { Name = "Recursos Humanos" };
        var ventas = new Department { Name = "Ventas" };
        var operaciones = new Department { Name = "Operaciones" };
        db.Departments.AddRange(desarrollo, rrhh, ventas, operaciones);

        // --- Empleados ---
        Employee NewEmp(string first, string last, Department dept, string position, int yearsAgo) => new()
        {
            FirstName = first,
            LastName = last,
            Email = $"{first}.{last}@hria.local".ToLowerInvariant(),
            Department = dept,
            Position = position,
            HireDate = DateOnly.FromDateTime(now.AddYears(-yearsAgo)),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Empleados vinculados a los usuarios demo.
        var adminEmp = new Employee
        {
            FirstName = "Ana",
            LastName = "Admin",
            Email = "admin@hria.local",
            Department = rrhh,
            Position = "Responsable de RR. HH.",
            HireDate = DateOnly.FromDateTime(now.AddYears(-5)),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var demoEmp = new Employee
        {
            FirstName = "Eva",
            LastName = "Empleada",
            Email = "empleado@hria.local",
            Department = desarrollo,
            Position = "Desarrolladora",
            HireDate = DateOnly.FromDateTime(now.AddYears(-2)),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var e3 = NewEmp("Carlos", "Gomez", desarrollo, "Desarrollador Senior", 4);
        var e4 = NewEmp("Marta", "Ruiz", desarrollo, "QA Engineer", 3);
        var e5 = NewEmp("Luis", "Perez", ventas, "Comercial", 6);
        var e6 = NewEmp("Sara", "Lopez", ventas, "Comercial", 1);
        var e7 = NewEmp("Javier", "Moreno", operaciones, "Técnico de Operaciones", 2);
        var e8 = NewEmp("Lucia", "Diaz", operaciones, "Coordinadora", 7);
        var e9 = NewEmp("Pablo", "Sanz", rrhh, "Técnico de RR. HH.", 3);
        var e10 = NewEmp("Nuria", "Vidal", desarrollo, "Diseñadora UX", 2);

        var employees = new[] { adminEmp, demoEmp, e3, e4, e5, e6, e7, e8, e9, e10 };
        db.Employees.AddRange(employees);

        // --- Usuarios demo (solo admin y empleado) ---
        db.Users.AddRange(
            new User
            {
                Employee = adminEmp,
                Email = adminEmp.Email,
                PasswordHash = hasher.Hash(DemoPassword),
                Role = Role.Admin,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Employee = demoEmp,
                Email = demoEmp.Email,
                PasswordHash = hasher.Hash(DemoPassword),
                Role = Role.Employee,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

        // --- Jornadas ---
        // Jornadas completas de días anteriores para varios empleados.
        foreach (var emp in new[] { demoEmp, e3, e4, e5, e8 })
        {
            for (var d = 1; d <= 3; d++)
            {
                var day = today.AddDays(-d);
                var checkIn = day.ToDateTime(new TimeOnly(8, 0)).ToUniversalTime();
                var checkOut = day.ToDateTime(new TimeOnly(16, 30)).ToUniversalTime();
                var wd = new Workday
                {
                    Employee = emp,
                    Date = day,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    Status = WorkdayStatus.Completed,
                    CreatedAt = checkIn,
                    UpdatedAt = checkOut
                };
                wd.Breaks.Add(new Break
                {
                    StartTime = day.ToDateTime(new TimeOnly(12, 0)).ToUniversalTime(),
                    EndTime = day.ToDateTime(new TimeOnly(12, 30)).ToUniversalTime()
                });
                db.Workdays.Add(wd);
            }
        }

        // Empleados trabajando AHORA (jornada abierta, sin salida).
        foreach (var emp in new[] { e3, e6 })
        {
            db.Workdays.Add(new Workday
            {
                Employee = emp,
                Date = today,
                CheckIn = now.AddHours(-2),
                CheckOut = null,
                Status = WorkdayStatus.Open,
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-2)
            });
        }

        // Empleado EN DESCANSO ahora (jornada abierta + descanso abierto).
        var onBreak = new Workday
        {
            Employee = e7,
            Date = today,
            CheckIn = now.AddHours(-3),
            CheckOut = null,
            Status = WorkdayStatus.Open,
            CreatedAt = now.AddHours(-3),
            UpdatedAt = now.AddMinutes(-15)
        };
        onBreak.Breaks.Add(new Break { StartTime = now.AddMinutes(-15), EndTime = null });
        db.Workdays.Add(onBreak);

        // Jornadas INCOMPLETAS (entrada en un día pasado, sin salida).
        foreach (var emp in new[] { e9, e10 })
        {
            var day = today.AddDays(-2);
            db.Workdays.Add(new Workday
            {
                Employee = emp,
                Date = day,
                CheckIn = day.ToDateTime(new TimeOnly(9, 0)).ToUniversalTime(),
                CheckOut = null,
                Status = WorkdayStatus.Incomplete,
                CreatedAt = day.ToDateTime(new TimeOnly(9, 0)).ToUniversalTime(),
                UpdatedAt = day.ToDateTime(new TimeOnly(9, 0)).ToUniversalTime()
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
