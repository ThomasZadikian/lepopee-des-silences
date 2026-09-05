using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacterItem
{
    private PlayerCharacterItem(
        OwnedItemInstanceId id,
        string itemDefinitionKey,
        DateTimeOffset acquiredAtUtc,
        string? source,
        EquipmentPosition? position)
    {
        Id = id;
        ItemDefinitionKey = itemDefinitionKey;
        AcquiredAtUtc = acquiredAtUtc;
        Source = source;
        Position = position;
    }

    public OwnedItemInstanceId Id { get; }
    public string ItemDefinitionKey { get; }
    public DateTimeOffset AcquiredAtUtc { get; }
    public string? Source { get; }
    public bool IsEquipped => Position.HasValue;
    public EquipmentPosition? Position { get; private set; }

    [Obsolete("Use Position. Slot describes definition compatibility, not loadout placement.")]
    public EquipmentSlotKind Slot => Position switch
    {
        EquipmentPosition.MainWeapon => EquipmentSlotKind.Weapon,
        EquipmentPosition.Ring1 or EquipmentPosition.Ring2 => EquipmentSlotKind.Accessory,
        _ => EquipmentSlotKind.Relic
    };

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
            OwnedItemInstanceId.New(),
            itemDefinitionKey.Trim(),
            acquiredAtUtc,
            string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
            isEquipped ? LegacyPosition(slot) : null);
    }

    public static PlayerCharacterItem Rehydrate(
        OwnedItemInstanceId id,
        string itemDefinitionKey,
        DateTimeOffset acquiredAtUtc,
        string? source,
        EquipmentPosition? position)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException("Owned item instance id is required.");
        if (string.IsNullOrWhiteSpace(itemDefinitionKey))
            throw new DomainException("Item definition key is required.");

        return new PlayerCharacterItem(
            id,
            itemDefinitionKey.Trim(),
            acquiredAtUtc,
            string.IsNullOrWhiteSpace(source) ? null : source.Trim(),
            position);
    }

    internal void Equip(EquipmentPosition position) => Position = position;

    internal void Unequip() => Position = null;

    private static EquipmentPosition LegacyPosition(EquipmentSlotKind slot) => slot switch
    {
        EquipmentSlotKind.MainWeapon or EquipmentSlotKind.Weapon => EquipmentPosition.MainWeapon,
        EquipmentSlotKind.Ring or EquipmentSlotKind.Accessory => EquipmentPosition.Ring1,
        EquipmentSlotKind.Head => EquipmentPosition.Head,
        EquipmentSlotKind.Neck => EquipmentPosition.Neck,
        EquipmentSlotKind.Shoulders => EquipmentPosition.Shoulders,
        EquipmentSlotKind.Cape => EquipmentPosition.Cape,
        EquipmentSlotKind.Chest => EquipmentPosition.Chest,
        EquipmentSlotKind.Wrist => EquipmentPosition.Wrist,
        EquipmentSlotKind.Hand => EquipmentPosition.Hand,
        EquipmentSlotKind.Waist => EquipmentPosition.Waist,
        EquipmentSlotKind.Legs => EquipmentPosition.Legs,
        EquipmentSlotKind.Feet => EquipmentPosition.Feet,
        EquipmentSlotKind.OffWeapon => EquipmentPosition.OffWeapon,
        _ => EquipmentPosition.Relic
    };
}
