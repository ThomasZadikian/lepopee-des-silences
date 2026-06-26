namespace Leds.GameEngine.Domain.Combats.StatusEffects;

public enum StatusEffectKind
{
    DamageOverTime, // typed damage applied each tick interval (poison/burn)
    HealOverTime,   // vitality restored each tick interval (regen)
    StatModifier,   // buff/debuff a CombatStat (Magnitude may be negative)
    Stun,           // cannot act AND gauge frozen
    Silence,        // cannot use tagged spells (basic attack still allowed)
    AtbLock         // gauge frozen (cannot fill) but may act if already ready
}