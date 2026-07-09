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
    DotDamageReduction = 6,
    // Percentage (Amount) added to the wearer's StatKind stat, computed against that
    // stat's base value at run start (e.g. Bague du courage: +10% Speed, +10%
    // AttackPower). This is now the default way to author stat-boosting equipment —
    // prefer this over the flat StatBonus for new items.
    StatBonusPercent = 7
}
