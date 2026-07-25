using System.Security.Cryptography;
using HRIA.Application.Common.Interfaces;

namespace HRIA.Infrastructure.Security;

/// <summary>
/// Hash de contraseñas con PBKDF2 (HMAC-SHA256), sal aleatoria por contraseña.
/// Formato almacenado: "{iteraciones}.{saltBase64}.{hashBase64}".
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;   // 128 bits
    private const int KeySize = 32;    // 256 bits

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        // Comparación en tiempo constante.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
