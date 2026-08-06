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
    StatBonusPercent = 7,
    // Percentage points (Amount) added to the wearer's damage with Magic-category
    // skills (e.g. Pomenian's monocle: +10% offensive spell damage).
    MagicDamageBonusPercent = 8,
    // Percentage points (Amount, 0-100) by which incoming Magic-category damage is
    // reduced for the wearer.
    MagicDamageReductionPercent = 9,
    // Percentage points (Amount) added directly to the wearer's critical hit chance,
    // on top of the chance derived from Focus (CriticalHitCalibration) — still capped
    // by CriticalHitCalibration.MaxCritChance overall.
    CriticalChanceBonusPercent = 10,
    // Percentage points (Amount) added to the DAMAGE DEALT by DamageOverTime effects
    // the wearer applies to others (e.g. l'Écrivain's Plume d'écrivain: +5%). Distinct
    // from DotDamageReduction, which reduces incoming DoT damage taken.
    DotDamageBonusPercent = 11,
    // Percentage points (Amount) added to ALL healing the wearer applies — skills and
    // items, in and out of combat (e.g. Majordome's legendary "La tasse du majordome": +15%).
    HealingBonusPercent = 12,
    // Overrides the matrix outcome for one incoming emotional register while equipped.
    // AffinityOutcome and Priority are mandatory; a higher priority wins.
    AffinityOutcomeOverride = 13,
    // Adjusts the final emotional multiplier by Amount percentage points after the
    // categorical outcome has been resolved. The source remains local to the wearer.
    AffinityMultiplierPercent = 14,
    // Stable runtime handler selected by Catalog data. This replaces branching on
    // item definition keys in the combat engine for bespoke passive mechanics.
    RuntimeBehavior = 15
}
