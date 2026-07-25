using HRIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRIA.Tests.Integration;

/// <summary>
/// Fábrica de la aplicación para pruebas de integración: sustituye SQL Server por
/// una base en memoria (compartida) y usa el entorno Development (secreto JWT efímero
/// y seeding demo). Permite probar el pipeline HTTP real sin depender de un servidor de BD.
/// </summary>
public class HriaWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Elimina el registro de EF con SQL Server.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(o =>
                o.UseInMemoryDatabase("hria-integration-tests"));
        });
    }
}
