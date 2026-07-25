using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HRIA.Application.Common.Security;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using HRIA.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HRIA.Tests.Security;

public class JwtTokenGeneratorTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "HRIA",
        Audience = "HRIA.Client",
        Secret = "test_secret_key_that_is_long_enough_for_hs256_123456",
        ExpiresMinutes = 60
    };

    private static TokenValidationParameters ValidationParams() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Options.Issuer,
        ValidAudience = Options.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = "sub"
    };

    // Igual que en Program.cs: no se mapean los nombres de claim entrantes.
    private static JwtSecurityTokenHandler Handler() => new() { MapInboundClaims = false };

    private static User SampleUser() => new()
    {
        Id = 7,
        EmployeeId = 42,
        Email = "admin@hria.local",
        Role = Role.Admin,
        IsActive = true
    };

    [Fact]
    public void Generate_ProducesValidTokenWithExpectedClaims()
    {
        var gen = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(Options));

        var (token, expiresAt) = gen.Generate(SampleUser());

        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var principal = Handler().ValidateToken(token, ValidationParams(), out _);

        principal.FindFirst("sub")!.Value.Should().Be("7");
        principal.FindFirst("employeeId")!.Value.Should().Be("42");
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Admin");
    }

    [Fact]
    public void ExpiredToken_FailsValidation()
    {
        // Token ya caducado firmado con el mismo secreto.
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret)), SecurityAlgorithms.HmacSha256);
        var expired = new JwtSecurityToken(
            Options.Issuer, Options.Audience,
            claims: new[] { new Claim("sub", "7") },
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds);
        var tokenStr = Handler().WriteToken(expired);

        var act = () => Handler().ValidateToken(tokenStr, ValidationParams(), out _);

        act.Should().Throw<SecurityTokenExpiredException>();
    }

    [Fact]
    public void Token_WithWrongSignature_FailsValidation()
    {
        var gen = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(Options));
        var (token, _) = gen.Generate(SampleUser());

        var tampered = ValidationParams();
        tampered.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("otro_secreto_distinto_pero_igual_de_largo_123456"));

        var act = () => Handler().ValidateToken(token, tampered, out _);

        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }
}
