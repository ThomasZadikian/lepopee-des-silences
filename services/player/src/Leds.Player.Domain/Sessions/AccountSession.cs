using System.Security.Cryptography;
using System.Text;
using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Sessions;

public sealed class AccountSession
{
    private AccountSession(
        Guid accountId,
        Guid sessionId,
        string refreshTokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? rotatedAtUtc = null,
        DateTimeOffset? revokedAtUtc = null)
    {
        AccountId = accountId;
        SessionId = sessionId;
        RefreshTokenHash = refreshTokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RotatedAtUtc = rotatedAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    public Guid AccountId { get; }
    public Guid SessionId { get; }
    public string RefreshTokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RotatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsRevoked => RevokedAtUtc.HasValue;

    public static AccountSession Create(
        Guid accountId,
        Guid sessionId,
        string refreshTokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Validate(accountId, sessionId, refreshTokenHash, createdAtUtc, expiresAtUtc);
        return new AccountSession(accountId, sessionId, refreshTokenHash, createdAtUtc, expiresAtUtc);
    }

    public static AccountSession Rehydrate(
        Guid accountId,
        Guid sessionId,
        string refreshTokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? rotatedAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        Validate(accountId, sessionId, refreshTokenHash, createdAtUtc, expiresAtUtc);
        return new AccountSession(
            accountId,
            sessionId,
            refreshTokenHash,
            createdAtUtc,
            expiresAtUtc,
            rotatedAtUtc,
            revokedAtUtc);
    }

    public void RotateRefreshToken(
        string newRefreshTokenHash,
        DateTimeOffset newExpiresAtUtc,
        DateTimeOffset rotatedAtUtc)
    {
        if (IsRevoked)
            throw new DomainException("A revoked session cannot rotate its refresh token.");
        if (string.IsNullOrWhiteSpace(newRefreshTokenHash))
            throw new DomainException("Refresh-token hash is required.");
        if (newExpiresAtUtc <= rotatedAtUtc)
            throw new DomainException("Refresh-token expiration must be after rotation.");

        RefreshTokenHash = newRefreshTokenHash;
        ExpiresAtUtc = newExpiresAtUtc;
        RotatedAtUtc = rotatedAtUtc;
    }

    public bool MatchesRefreshTokenHash(string presentedHash)
    {
        if (IsRevoked || string.IsNullOrEmpty(presentedHash))
            return false;

        var expected = Encoding.UTF8.GetBytes(RefreshTokenHash);
        var presented = Encoding.UTF8.GetBytes(presentedHash);
        return expected.Length == presented.Length && CryptographicOperations.FixedTimeEquals(expected, presented);
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (IsRevoked)
            return;

        RevokedAtUtc = revokedAtUtc;
    }

    private static void Validate(
        Guid accountId,
        Guid sessionId,
        string refreshTokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Account id is required.");
        if (sessionId == Guid.Empty)
            throw new DomainException("Session id is required.");
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
            throw new DomainException("Refresh-token hash is required.");
        if (expiresAtUtc <= createdAtUtc)
            throw new DomainException("Session expiration must be after creation.");
    }
}
