using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerPermanentUnlock
{
    private PlayerPermanentUnlock(string unlockKey, string unlockType, Guid? sourceRunId, DateTimeOffset unlockedAtUtc)
    {
        UnlockKey = unlockKey;
        UnlockType = unlockType;
        SourceRunId = sourceRunId;
        UnlockedAtUtc = unlockedAtUtc;
    }

    public string UnlockKey { get; }
    public string UnlockType { get; }
    public Guid? SourceRunId { get; }
    public DateTimeOffset UnlockedAtUtc { get; }

    public static PlayerPermanentUnlock Create(string unlockKey, string unlockType, Guid? sourceRunId, DateTimeOffset unlockedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(unlockKey)) throw new DomainException("Unlock key is required.");
        if (string.IsNullOrWhiteSpace(unlockType)) throw new DomainException("Unlock type is required.");

        return new PlayerPermanentUnlock(unlockKey.Trim(), unlockType.Trim(), sourceRunId, unlockedAtUtc);
    }
}
