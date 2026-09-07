namespace Leds.Player.Domain.Players;

/// <summary>Semantic kind accepted by an item definition.</summary>
public enum EquipmentSlotKind
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
    Ring = 11,
    Relic = 12,
    MainWeapon = 13,
    OffWeapon = 14,

    // Transitional wire aliases. New code must use the explicit vocabulary above.
    [Obsolete("Use MainWeapon.")]
    Weapon = 101,
    [Obsolete("Use an explicit slot kind such as Ring, Cape or Wrist.")]
    Accessory = 102
}
