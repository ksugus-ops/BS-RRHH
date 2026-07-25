using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRIA.Infrastructure.Persistence;

/// <summary>
/// Factory de diseño para las herramientas de EF Core (migraciones).
/// Fija el proveedor SQL Server con independencia del entorno de ejecución,
/// de modo que las migraciones se generen siempre para SQL Server.
/// La cadena de conexión solo se usa para seleccionar el proveedor; no se conecta.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=HRIA;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options);
    }
}
