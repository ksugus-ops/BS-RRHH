using FluentAssertions;
using HRIA.Infrastructure.Security;
using Xunit;

namespace HRIA.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesVerifiableHash()
    {
        var hash = _hasher.Hash("Demo1234!");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotContain("Demo1234!"); // nunca en texto plano
        _hasher.Verify("Demo1234!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Demo1234!");
        _hasher.Verify("otra-clave", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_UsesRandomSalt_DifferentHashesForSamePassword()
    {
        var h1 = _hasher.Hash("Demo1234!");
        var h2 = _hasher.Hash("Demo1234!");
        h1.Should().NotBe(h2); // sal aleatoria
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        _hasher.Verify("x", "formato-invalido").Should().BeFalse();
    }

    /// <summary>
    /// El hash incluido en el script de datos de demostración (db/03-seed-demo.sql)
    /// debe ser válido para la contraseña demo. Si este test falla, el script no
    /// permitiría iniciar sesión.
    /// </summary>
    [Fact]
    public void Verify_SeedScriptHash_IsValidForDemoPassword()
    {
        const string seedHash =
            "100000.WqdvmnxDjI8uKe85rD4O9w==.+jYPMHu04rG7iqrwej0oqxJn6NdNRthR9dhiwl75F6g=";

        _hasher.Verify("Demo1234!", seedHash).Should().BeTrue();
        _hasher.Verify("otra-clave", seedHash).Should().BeFalse();
    }
}
