using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

// The permanent backpack itself (SFD "Système d'équipement et sac permanent" § 6) — unlimited
// storage, survives between runs. Distinct from PlayerCharacterItem, which only models the
// per-character *equipped* subset; an item must be here before it can be equipped.
public sealed class PlayerPermanentItem
{
    private PlayerPermanentItem(
        string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc,
        string? containedLiquidDefinitionKey = null)
    {
        ItemDefinitionKey = itemDefinitionKey;
        SourceRunId = sourceRunId;
        AcquiredAtUtc = acquiredAtUtc;
        ContainedLiquidDefinitionKey = containedLiquidDefinitionKey;
    }

    public string ItemDefinitionKey { get; }
    public Guid? SourceRunId { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public string? ContainedLiquidDefinitionKey { get; private set; }

    public static PlayerPermanentItem Create(string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");

        return new PlayerPermanentItem(itemDefinitionKey.Trim(), sourceRunId, acquiredAtUtc);
    }

    public static PlayerPermanentItem Rehydrate(
        string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc,
        string? containedLiquidDefinitionKey)
        => new(itemDefinitionKey, sourceRunId, acquiredAtUtc, containedLiquidDefinitionKey);

    // Caller (application layer) is responsible for validating against the catalog that this
    // item is actually a container before calling — this domain object has no catalog access.
    public void SetContainedLiquid(string liquidDefinitionKey)
    {
        if (string.IsNullOrWhiteSpace(liquidDefinitionKey))
            throw new DomainException("Liquid definition key is required.");
        if (ContainedLiquidDefinitionKey is not null)
            throw new DomainException($"Item '{ItemDefinitionKey}' already holds a liquid — empty it first.");

        ContainedLiquidDefinitionKey = liquidDefinitionKey.Trim();
    }

    public void ClearContainedLiquid()
    {
        if (ContainedLiquidDefinitionKey is null)
            throw new DomainException($"Item '{ItemDefinitionKey}' is already empty.");

        ContainedLiquidDefinitionKey = null;
    }
}
