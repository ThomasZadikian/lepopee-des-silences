namespace Leds.GameEngine.Domain.Combats.StatusEffects;

public enum StatusEffectKind
{
    DamageOverTime, // typed damage applied each tick interval (poison/burn)
    HealOverTime,   // vitality restored each tick interval (regen)
    GuardOverTime,  // Guard granted each tick interval, independent of StatModifier/CombatStat
    StatModifier,   // buff/debuff a CombatStat (Magnitude may be negative)
    Stun,           // neutralizes the holder's activation
    Silence,        // prevents skill use for the holder's activation
    SkillGrant      // temporarily adds a snapshot of another combatant's skills (see CombatStatusEffect.GrantedSkills)
}
