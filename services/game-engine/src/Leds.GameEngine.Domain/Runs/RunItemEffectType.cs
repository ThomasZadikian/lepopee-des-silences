namespace Leds.GameEngine.Domain.Runs;

public enum RunItemEffectType
{
    None = 0,
    Heal = 1,
    Guard = 2,
    ManaRestore = 3,
    ChargeRestore = 4,
    NextCombatGuard = 5,
    NarrativeFragment = 6,
    AttackTypeOverride = 7,
    // Passive team-wide Speed bonus for as long as the item is granted this run
    // (e.g. Rêve d'Erina: +5% team Speed). EffectAmount is a whole percentage.
    TeamSpeedBonus = 8
}