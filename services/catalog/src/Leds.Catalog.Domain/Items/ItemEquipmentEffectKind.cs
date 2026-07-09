namespace Leds.Catalog.Domain.Items;

public enum ItemEquipmentEffectKind
{
    StatBonus = 0,
    GrantSkill = 1,
    GrantAffinity = 2,
    // Percentage reduction (Amount, 0-100) to damage of the given type (AffinityRegister)
    // taken by the wearer — independent of the categorical weak/resist/immune system.
    DamageReductionByType = 3,
    // Percentage points (Amount) added to the wearer's chance to hit with damaging skills.
    HitChanceBonus = 4,
    // Percentage (Amount, 0-100) by which incoming DamageOverTime effects have their
    // duration shortened for the wearer.
    DotDurationReduction = 5,
    // Percentage (Amount, 0-100) by which incoming DamageOverTime effects have their
    // per-tick damage reduced for the wearer.
    DotDamageReduction = 6
}
