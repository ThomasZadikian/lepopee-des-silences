using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Privacy;
using Leds.Player.Domain.Sessions;

namespace Leds.Player.Application.Abstractions;

public sealed record SecurityTokenState(
    Guid Id,
    Guid AccountId,
    string Purpose,
    string TokenHash,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ConsumedAtUtc)
{
    public bool CanConsume(DateTimeOffset now) => !ConsumedAtUtc.HasValue && now < ExpiresAtUtc;
}

public sealed record GameLeaseClaimResult(
    ActiveGameSessionLease Lease,
    bool Acquired,
    bool TransferRequired);

public interface IAccountStore
{
    Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken);
    Task<UserIdentity?> FindIdentityByEmailAsync(EmailAddress email, CancellationToken cancellationToken);
    Task<UserIdentity?> FindIdentityByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task RegisterAsync(
        PlayerProfile profile,
        UserIdentity identity,
        string verificationTokenHash,
        DateTimeOffset verificationTokenExpiresAtUtc,
        CancellationToken cancellationToken);

    Task SaveIdentityAsync(UserIdentity identity, CancellationToken cancellationToken);

    Task StoreSecurityTokenAsync(
        Guid accountId,
        string purpose,
        string tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken);

    Task<SecurityTokenState?> FindSecurityTokenAsync(
        string purpose,
        string tokenHash,
        CancellationToken cancellationToken);

    Task ConsumeSecurityTokenAsync(Guid tokenId, DateTimeOffset consumedAtUtc, CancellationToken cancellationToken);

    Task AddSessionAsync(AccountSession session, CancellationToken cancellationToken);
    Task<AccountSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AccountSession>> ListSessionsAsync(Guid accountId, CancellationToken cancellationToken);
    Task SaveSessionAsync(AccountSession session, CancellationToken cancellationToken);
    Task RevokeSessionsAsync(Guid accountId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PrivacyConsent>> ListConsentsAsync(Guid accountId, CancellationToken cancellationToken);
    Task SaveConsentAsync(Guid accountId, PrivacyConsent consent, CancellationToken cancellationToken);

    Task<AccountClosureRequest?> GetClosureRequestAsync(Guid accountId, CancellationToken cancellationToken);
    Task SaveClosureRequestAsync(AccountClosureRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ListExecutableClosureAccountIdsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task PurgeAuthenticationMaterialAsync(
        Guid accountId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken);

    Task<ActiveGameSessionLease?> GetGameLeaseAsync(Guid accountId, CancellationToken cancellationToken);
    Task SaveGameLeaseAsync(ActiveGameSessionLease lease, CancellationToken cancellationToken);
    Task<GameLeaseClaimResult> ClaimGameLeaseAsync(
        Guid accountId,
        Guid sessionId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        bool allowTransfer,
        CancellationToken cancellationToken);
    Task<bool> HeartbeatGameLeaseAsync(
        Guid accountId,
        Guid sessionId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task ReleaseGameLeaseAsync(Guid accountId, Guid sessionId, CancellationToken cancellationToken);
}
