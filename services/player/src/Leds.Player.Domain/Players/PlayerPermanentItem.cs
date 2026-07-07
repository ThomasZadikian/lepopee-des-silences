using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

// The permanent backpack itself (SFD "Système d'équipement et sac permanent" § 6) — unlimited
// storage, survives between runs. Distinct from PlayerCharacterItem, which only models the
// per-character *equipped* subset; an item must be here before it can be equipped.
public sealed class PlayerPermanentItem
{
    private PlayerPermanentItem(string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc)
    {
        ItemDefinitionKey = itemDefinitionKey;
        SourceRunId = sourceRunId;
        AcquiredAtUtc = acquiredAtUtc;
    }

    public string ItemDefinitionKey { get; }
    public Guid? SourceRunId { get; }
    public DateTimeOffset AcquiredAtUtc { get; }

    public static PlayerPermanentItem Create(string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");

        return new PlayerPermanentItem(itemDefinitionKey.Trim(), sourceRunId, acquiredAtUtc);
    }
}
