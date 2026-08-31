using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Sessions;

public sealed class ActiveGameSessionLease
{
    private ActiveGameSessionLease(
        Guid accountId,
        Guid ownerSessionId,
        DateTimeOffset acquiredAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        AccountId = accountId;
        OwnerSessionId = ownerSessionId;
        AcquiredAtUtc = acquiredAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AccountId { get; }
    public Guid OwnerSessionId { get; private set; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public static ActiveGameSessionLease Acquire(
        Guid accountId,
        Guid ownerSessionId,
        DateTimeOffset now,
        TimeSpan leaseDuration)
    {
        ValidateIdsAndDuration(accountId, ownerSessionId, leaseDuration);
        return new ActiveGameSessionLease(accountId, ownerSessionId, now, now.Add(leaseDuration));
    }

    public static ActiveGameSessionLease Rehydrate(
        Guid accountId,
        Guid ownerSessionId,
        DateTimeOffset acquiredAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (accountId == Guid.Empty || ownerSessionId == Guid.Empty)
            throw new DomainException("Account and game-session owner ids are required.");
        if (expiresAtUtc <= acquiredAtUtc)
            throw new DomainException("Game-session lease expiration must follow acquisition.");

        return new ActiveGameSessionLease(accountId, ownerSessionId, acquiredAtUtc, expiresAtUtc);
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public void Heartbeat(Guid sessionId, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (sessionId != OwnerSessionId)
            throw new DomainException("Only the active game session may renew the lease.");
        if (leaseDuration <= TimeSpan.Zero)
            throw new DomainException("Game-session lease duration must be positive.");
        if (IsExpired(now))
            throw new DomainException("An expired game-session lease must be reacquired.");

        ExpiresAtUtc = now.Add(leaseDuration);
    }

    public void Transfer(Guid newOwnerSessionId, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (newOwnerSessionId == Guid.Empty)
            throw new DomainException("New game-session owner is required.");
        if (leaseDuration <= TimeSpan.Zero)
            throw new DomainException("Game-session lease duration must be positive.");

        OwnerSessionId = newOwnerSessionId;
        ExpiresAtUtc = now.Add(leaseDuration);
    }

    private static void ValidateIdsAndDuration(Guid accountId, Guid ownerSessionId, TimeSpan leaseDuration)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Account id is required.");
        if (ownerSessionId == Guid.Empty)
            throw new DomainException("Game-session owner is required.");
        if (leaseDuration <= TimeSpan.Zero)
            throw new DomainException("Game-session lease duration must be positive.");
    }
}
