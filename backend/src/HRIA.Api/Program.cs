using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using HRIA.Api.Common;
using HRIA.Api.Middleware;
using HRIA.Application;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Common.Security;
using HRIA.Infrastructure;
using HRIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración base ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Capas Application e Infrastructure.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Validación automática con FluentValidation (rellena ModelState -> 400).
builder.Services.AddFluentValidationAutoValidation();

// --- JWT ---
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    if (builder.Environment.IsDevelopment())
    {
        // Secreto efímero solo para desarrollo (no se versiona; cambia en cada arranque).
        jwtOptions.Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }
    else
    {
        throw new InvalidOperationException("Jwt:Secret es obligatorio (configúralo por variable de entorno).");
    }
}
// Asegura que el mismo secreto resuelto se usa en la generación de tokens.
builder.Services.PostConfigure<JwtOptions>(o => o.Secret = jwtOptions.Secret);

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // conserva los nombres de claim originales ("sub", "employeeId").
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// --- Rate limiting (login e IA), particionado por IP ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("ai", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// --- CORS restrictivo configurable ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("HriaCors", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

// --- Swagger con soporte de Bearer ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HRIA API",
        Version = "v1",
        Description = "ERP de RR. HH. con asistente de IA — Trabajo de Fin de Máster."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT (sin el prefijo 'Bearer ').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// --- Pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("HriaCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Migración y seeding de datos demo ---
await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();

    // SQL Server usa migraciones; SQLite (dev/demo) e In-Memory crean el esquema directamente.
    if (db.Database.IsSqlServer())
        await db.Database.MigrateAsync();
    else if (db.Database.IsRelational())
        await db.Database.EnsureCreatedAsync();

    var demoEnabled = app.Configuration.GetValue("Demo:Enabled", true);
    if (demoEnabled)
    {
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher);
    }
}

// Necesario para las pruebas de integración con WebApplicationFactory.
public partial class Program { }
