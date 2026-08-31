using System.Security.Cryptography;
using System.Text;
using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Common;

namespace Leds.Player.Application.Accounts;

public sealed class DefaultRecoveryCodeService : IRecoveryCodeService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public RecoveryCodeBatch Generate(int count = 10)
    {
        if (count is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(count), "Recovery-code count must be between 1 and 20.");

        var rawCodes = new HashSet<string>(StringComparer.Ordinal);
        while (rawCodes.Count < count)
            rawCodes.Add(CreateCode());

        var ordered = rawCodes.ToArray();
        return new RecoveryCodeBatch(ordered, ordered.Select(Hash).ToArray());
    }

    public string Hash(string rawCode)
    {
        var normalized = Normalize(rawCode);
        if (normalized.Length < 12)
            throw new DomainException("Recovery code is invalid.");

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            .ToLowerInvariant();
    }

    private static string CreateCode()
    {
        Span<byte> random = stackalloc byte[16];
        RandomNumberGenerator.Fill(random);
        Span<char> characters = stackalloc char[20];
        for (var i = 0; i < characters.Length; i++)
            characters[i] = Alphabet[random[i % random.Length] % Alphabet.Length];

        return string.Concat(
            characters[..5], "-",
            characters.Slice(5, 5), "-",
            characters.Slice(10, 5), "-",
            characters.Slice(15, 5));
    }

    private static string Normalize(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            throw new DomainException("Recovery code is required.");

        return new string(rawCode
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
