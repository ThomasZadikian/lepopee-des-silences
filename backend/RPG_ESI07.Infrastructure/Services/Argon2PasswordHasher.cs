using Konscious.Security.Cryptography;
using RPG_ESI07.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RPG_ESI07.Infrastructure.Services;

public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 3;
    private const int MemorySize = 65536;
    private const int DegreeOfParallelism = 4;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty", nameof(password));

        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var hash = HashPasswordWithSalt(password, salt);

        var combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;

        if (string.IsNullOrWhiteSpace(hashedPassword)) return false;

        try
        {
            var combined = Convert.FromBase64String(hashedPassword);

            if (combined.Length != SaltSize + HashSize)
                return false;

            var salt = new byte[SaltSize];
            var hash = new byte[HashSize];
            Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(combined, SaltSize, hash, 0, HashSize);

            var computedHash = HashPasswordWithSalt(password, salt);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        return argon2.GetBytes(HashSize);
    }
}