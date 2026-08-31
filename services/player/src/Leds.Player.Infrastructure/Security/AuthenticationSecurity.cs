using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Microsoft.Extensions.Configuration;

namespace Leds.Player.Infrastructure.Security;

public sealed class AuthenticationSecurity : IAuthenticationSecurity
{
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int ArgonMemoryKiB = 65536;
    private const int ArgonIterations = 3;
    private const int ArgonParallelism = 2;
    private const int TotpPeriodSeconds = 30;
    private const int TotpDigits = 6;

    private readonly byte[] _mfaProtectionKey;
    private readonly string _issuer;

    public AuthenticationSecurity(IConfiguration configuration)
    {
        var protectionKey = configuration["Authentication:MfaProtectionKey"];
        if (string.IsNullOrWhiteSpace(protectionKey))
            throw new InvalidOperationException("Authentication:MfaProtectionKey must be configured.");

        try
        {
            _mfaProtectionKey = Convert.FromBase64String(protectionKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Authentication:MfaProtectionKey must be base64 encoded.", exception);
        }

        if (_mfaProtectionKey.Length != 32)
            throw new InvalidOperationException("Authentication:MfaProtectionKey must decode to exactly 32 bytes.");

        _issuer = configuration["Authentication:TotpIssuer"]?.Trim() is { Length: > 0 } issuer
            ? issuer
            : "L'épopée des silences";
    }

    public string HashPassword(string password)
    {
        PasswordPolicy.EnsureAcceptable(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = DeriveArgon2(password, salt, ArgonMemoryKiB, ArgonIterations, ArgonParallelism);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"argon2id$v=19$m={ArgonMemoryKiB},t={ArgonIterations},p={ArgonParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            var parts = passwordHash.Split('$', StringSplitOptions.None);
            if (parts.Length != 5 || parts[0] != "argon2id" || parts[1] != "v=19")
                return false;

            var parameters = parts[2].Split(',')
                .Select(value => value.Split('=', 2))
                .ToDictionary(value => value[0], value => int.Parse(value[1], CultureInfo.InvariantCulture));
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = DeriveArgon2(password, salt, parameters["m"], parameters["t"], parameters["p"]);

            return expected.Length == actual.Length
                && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (Exception exception) when (
            exception is FormatException
                or IndexOutOfRangeException
                or KeyNotFoundException
                or OverflowException)
        {
            return false;
        }
    }

    public OpaqueToken GenerateOpaqueToken()
    {
        var raw = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new OpaqueToken(raw, HashOpaqueToken(raw));
    }

    public string HashOpaqueToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new DomainException("Security token is required.");

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }

    public MfaEnrollment CreateMfaEnrollment(EmailAddress email)
    {
        var secret = RandomNumberGenerator.GetBytes(20);
        var manualKey = Base32Encode(secret);
        var protectedSecret = ProtectSecret(secret);
        var uri = $"otpauth://totp/{Uri.EscapeDataString(_issuer)}:{Uri.EscapeDataString(email.Value)}" +
                  $"?secret={manualKey}&issuer={Uri.EscapeDataString(_issuer)}&algorithm=SHA1&digits={TotpDigits}&period={TotpPeriodSeconds}";

        return new MfaEnrollment(protectedSecret, uri, manualKey);
    }

    public bool VerifyTotp(string protectedSecret, string code, DateTimeOffset now)
    {
        if (!IsSixDigitCode(code))
            return false;

        byte[] secret;
        try
        {
            secret = UnprotectSecret(protectedSecret);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        var counter = now.ToUnixTimeSeconds() / TotpPeriodSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var expected = ComputeTotp(secret, counter + offset);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected),
                    Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] DeriveArgon2(
        string password,
        byte[] salt,
        int memorySize,
        int iterations,
        int degreeOfParallelism)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism
        };
        return argon.GetBytes(HashLength);
    }

    private string ProtectSecret(byte[] secret)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[secret.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_mfaProtectionKey, tagSizeInBytes: 16);
        aes.Encrypt(nonce, secret, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return Convert.ToBase64String(payload);
    }

    private byte[] UnprotectSecret(string protectedSecret)
    {
        var payload = Convert.FromBase64String(protectedSecret);
        if (payload.Length < 29)
            throw new CryptographicException("Invalid protected MFA secret.");

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var ciphertext = payload.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_mfaProtectionKey, tagSizeInBytes: 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    private static string ComputeTotp(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static bool IsSixDigitCode(string code) =>
        code.Length == TotpDigits && code.All(char.IsAsciiDigit);

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
            output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);

        return output.ToString();
    }
}
