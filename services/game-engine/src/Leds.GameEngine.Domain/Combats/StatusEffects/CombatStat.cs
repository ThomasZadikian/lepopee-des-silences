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
    CriticalChanceBonus
}