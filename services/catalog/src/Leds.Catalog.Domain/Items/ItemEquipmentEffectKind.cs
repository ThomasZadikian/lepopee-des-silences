namespace Leds.Catalog.Domain.Items;

public enum ItemEquipmentEffectKind
{
    StatBonus = 0,
    GrantSkill = 1,
    GrantAffinity = 2,
    // Percentage reduction (Amount, 0-100) to damage of the given type (AffinityRegister)
    // taken by the wearer — independent of the categorical weak/resist/immune system.
    DamageReductionByType = 3
}
