using HRIA.Application.Ai;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Common.Security;
using HRIA.Infrastructure.Ai;
using HRIA.Infrastructure.Persistence;
using HRIA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config.GetValue<string>("Database:Provider") ?? "SqlServer";
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // Alternativa sin dependencias externas para ejecución local/demo.
                options.UseSqlite(string.IsNullOrWhiteSpace(connectionString)
                    ? "Data Source=hria.db"
                    : connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Opciones JWT (sección "Jwt").
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Proveedor de IA "live". Se registra SOLO el seleccionado en "Ai:Provider":
        // con dos proveedores vivos en el contenedor, cuál gana dependería del orden
        // de registro, y eso es una sorpresa esperando a ocurrir en producción.
        // El servicio usa el demo como respaldo si no hay API key.
        services.Configure<ClaudeOptions>(config.GetSection(ClaudeOptions.SectionName));
        services.Configure<OpenAiOptions>(config.GetSection(OpenAiOptions.SectionName));

        var aiProvider = config.GetValue<string>("Ai:Provider") ?? "Claude";
        if (aiProvider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ClaudeAssistant>();
            services.AddScoped<IAiAssistant>(sp => sp.GetRequiredService<ClaudeAssistant>());
        }
        else if (aiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<OpenAiAssistant>();
            services.AddScoped<IAiAssistant>(sp => sp.GetRequiredService<OpenAiAssistant>());
        }
        // Cualquier otro valor ("Demo") deja el contenedor sin proveedor live.

        return services;
    }
}
