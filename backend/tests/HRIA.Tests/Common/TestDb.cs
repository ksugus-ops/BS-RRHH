using HRIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Tests.Common;

/// <summary>Utilidad para crear un AppDbContext en memoria aislado por test.</summary>
public static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hria-tests-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }
}
