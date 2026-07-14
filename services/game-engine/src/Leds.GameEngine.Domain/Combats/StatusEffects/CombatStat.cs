namespace Leds.GameEngine.Domain.Combats.StatusEffects;

public enum CombatStat
{
    None,
    AttackPower,
    Defense,
    Speed,
    Focus,
    // Authored base stats (mirror AttackPower/Defense) driving the Magic-category
    // damage ratio in CombatSkillEffectResolver.StatModifierDamageMultiplier —
    // symmetric to how AttackPower/Defense drive the Physical-category ratio.
    MagicAttack,
    MagicDefense,
    // Virtual stats (no authored base value — base is always 0, so only flat
    // StatModifier contributions matter): percentage points added to / subtracted
    // from Magic-category skill damage (see CombatSkillEffectResolver).
    MagicDamageBonus,
    MagicDamageReduction,
    CriticalChanceBonus,
    // Percentage points added to the caster's DamageOverTime damage dealt (see
    // Combatant.EffectiveDotDamageBonusPercent) — l'Écrivain's "Plume d'écrivain".
    DotDamageBonus,
    // Percentage modifier applied directly to ATB fill-per-tick (see
    // Combatant.RecalculateAtbFillPerTick), independent of the Speed stat — e.g.
    // "Une destinée cruelle" boosts Speed +20% (a stat) while separately slowing
    // the ATB gauge itself by -15% (this virtual stat).
    AtbTempoModifier,
    // Percentage points subtracted from a Player-side skill's mana/charge cost at cast
    // time (see CombatSkillEffectResolver.ConsumeResources) — e.g. Mina's "Protection
    // de Him'Lit" reduces cost by 5%. Negative-only in practice; a positive magnitude
    // would raise costs.
    SkillCostReductionPercent,
    // Percentage points added to all healing this combatant applies (skills and items —
    // see Combatant.EffectiveHealingBonusPercent), on top of the permanent equipment
    // component — e.g. Majordome's legendary "La tasse du majordome": +15%.
    HealingBonus
}