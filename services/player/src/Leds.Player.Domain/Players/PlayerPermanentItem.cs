using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

// Persistent registry of physical item instances — unlimited storage, survives between runs.
// PlayerCharacterItem carries the owning-character assignment as well as the optional equipment
// position. An instance must exist in both models before that character can equip it.
public sealed class PlayerPermanentItem
{
    private PlayerPermanentItem(
        OwnedItemInstanceId id, string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc,
        string? containedLiquidDefinitionKey = null)
    {
        Id = id;
        ItemDefinitionKey = itemDefinitionKey;
        SourceRunId = sourceRunId;
        AcquiredAtUtc = acquiredAtUtc;
        ContainedLiquidDefinitionKey = containedLiquidDefinitionKey;
    }

    public OwnedItemInstanceId Id { get; }
    public string ItemDefinitionKey { get; }
    public Guid? SourceRunId { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public string? ContainedLiquidDefinitionKey { get; private set; }

    public static PlayerPermanentItem Create(string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");

        return new PlayerPermanentItem(OwnedItemInstanceId.New(), itemDefinitionKey.Trim(), sourceRunId, acquiredAtUtc);
    }

    public static PlayerPermanentItem Create(
        OwnedItemInstanceId id, string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Owned item instance id is required.");
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");
        return new PlayerPermanentItem(id, itemDefinitionKey.Trim(), sourceRunId, acquiredAtUtc);
    }

    public static PlayerPermanentItem Rehydrate(
        OwnedItemInstanceId id, string itemDefinitionKey, Guid? sourceRunId, DateTimeOffset acquiredAtUtc,
        string? containedLiquidDefinitionKey)
        => new(id, itemDefinitionKey, sourceRunId, acquiredAtUtc, containedLiquidDefinitionKey);

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
