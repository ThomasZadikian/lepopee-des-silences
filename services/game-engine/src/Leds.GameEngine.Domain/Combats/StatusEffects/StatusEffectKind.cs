namespace Leds.GameEngine.Domain.Combats.StatusEffects;

public enum StatusEffectKind
{
    DamageOverTime, // typed damage applied each tick interval (poison/burn)
    HealOverTime,   // vitality restored each tick interval (regen)
    GuardOverTime,  // Guard granted each tick interval, independent of StatModifier/CombatStat
    StatModifier,   // buff/debuff a CombatStat (Magnitude may be negative)
    Stun,           // cannot act AND gauge frozen
    Silence,        // gates identically to Stun (see Combatant.IsAtbLocked): cannot act, gauge frozen
    AtbLock,        // gauge frozen (cannot fill) but may act if already ready
    SkillGrant,     // temporarily adds a snapshot of another combatant's skills (see CombatStatusEffect.GrantedSkills)
    GuaranteedCritical // single-use: the holder's next damage roll is forced to critical (boolean override, not a magnitude — see Loi de la Première Impression)
}