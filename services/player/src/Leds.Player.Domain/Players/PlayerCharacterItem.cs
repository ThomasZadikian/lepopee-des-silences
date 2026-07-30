using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacterItem
{
    private PlayerCharacterItem(
        string itemDefinitionKey,
        DateTimeOffset acquiredAtUtc,
        string? source,
        bool isEquipped,
        EquipmentSlotKind slot)
    {
        ItemDefinitionKey = itemDefinitionKey;
        AcquiredAtUtc = acquiredAtUtc;
        Source = source;
        IsEquipped = isEquipped;
        Slot = slot;
    }

    public string ItemDefinitionKey { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public string? Source { get; }
    public bool IsEquipped { get; private set; }
    public EquipmentSlotKind Slot { get; private set; }

    public static PlayerCharacterItem Create(
        string itemDefinitionKey,
        DateTimeOffset acquiredAtUtc,
        string? source = null,
        bool isEquipped = false,
        EquipmentSlotKind slot = EquipmentSlotKind.Relic)
    {
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");

        return new PlayerCharacterItem(
            itemDefinitionKey.Trim(),
            acquiredAtUtc,
            string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
            isEquipped,
            slot);
    }

    internal void Equip(EquipmentSlotKind slot)
    {
        Slot = slot;
        IsEquipped = true;
    }

    internal void Unequip() => IsEquipped = false;
}
