using System.Security.Cryptography;
using System.Text;
using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public sealed class EmailVerificationToken
{
    private EmailVerificationToken(Guid accountId, string tokenHash, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc)
    {
        AccountId = accountId;
        TokenHash = tokenHash;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AccountId { get; }
    public string TokenHash { get; }
    public DateTimeOffset IssuedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public static EmailVerificationToken Issue(Guid accountId, string tokenHash, DateTimeOffset issuedAtUtc, TimeSpan lifetime)
    {
        ValidateIssue(tokenHash, lifetime);
        return new EmailVerificationToken(accountId, tokenHash, issuedAtUtc, issuedAtUtc.Add(lifetime));
    }

    public bool TryConsume(string presentedTokenHash, DateTimeOffset now)
    {
        if (ConsumedAtUtc.HasValue || now >= ExpiresAtUtc || !SecurityTokenHash.Matches(TokenHash, presentedTokenHash))
            return false;

        ConsumedAtUtc = now;
        return true;
    }

    private static void ValidateIssue(string tokenHash, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash is required.");
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException("Token lifetime must be positive.");
    }
}

public sealed class PasswordResetToken
{
    private PasswordResetToken(Guid accountId, string tokenHash, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc)
    {
        AccountId = accountId;
        TokenHash = tokenHash;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AccountId { get; }
    public string TokenHash { get; }
    public DateTimeOffset IssuedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public static PasswordResetToken Issue(Guid accountId, string tokenHash, DateTimeOffset issuedAtUtc, TimeSpan lifetime)
    {
        ValidateIssue(tokenHash, lifetime);
        return new PasswordResetToken(accountId, tokenHash, issuedAtUtc, issuedAtUtc.Add(lifetime));
    }

    public bool TryConsume(string presentedTokenHash, DateTimeOffset now)
    {
        if (ConsumedAtUtc.HasValue || now >= ExpiresAtUtc || !SecurityTokenHash.Matches(TokenHash, presentedTokenHash))
            return false;

        ConsumedAtUtc = now;
        return true;
    }

    private static void ValidateIssue(string tokenHash, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash is required.");
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException("Token lifetime must be positive.");
    }
}

internal static class SecurityTokenHash
{
    public static bool Matches(string expectedHash, string presentedHash)
    {
        if (string.IsNullOrEmpty(presentedHash))
            return false;

        var expected = Encoding.UTF8.GetBytes(expectedHash);
        var presented = Encoding.UTF8.GetBytes(presentedHash);
        return expected.Length == presented.Length && CryptographicOperations.FixedTimeEquals(expected, presented);
    }
}
