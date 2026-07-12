namespace Leds.GameEngine.Domain.Combats.StatusEffects;

public enum CombatStat
{
    None,
    AttackPower,
    Defense,
    Speed,
    Focus,
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
    AtbTempoModifier
}