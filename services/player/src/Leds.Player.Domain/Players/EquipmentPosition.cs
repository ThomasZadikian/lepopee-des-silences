namespace Leds.Player.Domain.Players;

/// <summary>Concrete position in a character loadout.</summary>
public enum EquipmentPosition
{
    Head = 1,
    Neck = 2,
    Shoulders = 3,
    Cape = 4,
    Chest = 5,
    Wrist = 6,
    Hand = 7,
    Waist = 8,
    Legs = 9,
    Feet = 10,
    Ring1 = 11,
    Ring2 = 12,
    Relic = 13,
    MainWeapon = 14,
    OffWeapon = 15
}

public static class EquipmentPositionCompatibility
{
    public static bool Accepts(EquipmentPosition position, EquipmentSlotKind slot) =>
        position switch
        {
            EquipmentPosition.Ring1 or EquipmentPosition.Ring2 => slot == EquipmentSlotKind.Ring,
            EquipmentPosition.Head => slot == EquipmentSlotKind.Head,
            EquipmentPosition.Neck => slot == EquipmentSlotKind.Neck,
            EquipmentPosition.Shoulders => slot == EquipmentSlotKind.Shoulders,
            EquipmentPosition.Cape => slot == EquipmentSlotKind.Cape,
            EquipmentPosition.Chest => slot == EquipmentSlotKind.Chest,
            EquipmentPosition.Wrist => slot == EquipmentSlotKind.Wrist,
            EquipmentPosition.Hand => slot == EquipmentSlotKind.Hand,
            EquipmentPosition.Waist => slot == EquipmentSlotKind.Waist,
            EquipmentPosition.Legs => slot == EquipmentSlotKind.Legs,
            EquipmentPosition.Feet => slot == EquipmentSlotKind.Feet,
            EquipmentPosition.Relic => slot == EquipmentSlotKind.Relic,
            EquipmentPosition.MainWeapon => slot is EquipmentSlotKind.MainWeapon or EquipmentSlotKind.Weapon,
            EquipmentPosition.OffWeapon => slot == EquipmentSlotKind.OffWeapon,
            _ => false
        };
}
