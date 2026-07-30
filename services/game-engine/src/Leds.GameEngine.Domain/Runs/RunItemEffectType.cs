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
    TeamSpeedBonus = 8,
    // Restores the same whole percentage of both max Vitality and max Mana at
    // once (e.g. Majordome's Tasse de thé: 35% PV + 35% PP). EffectAmount is
    // that shared percentage.
    HealAndManaRestorePercent = 9,

    // Canonical Palace consumables. Keeping these semantics explicit prevents
    // catalog loot from degrading into a generic zero-point heal.
    HealPercent = 10,
    ConditionalHealOrPoison = 11,
    HealPercentAndCleanseDot = 12,
    HealPercentAndSilence = 13,
    RevivePercent = 14,
    HealPercentAndEvasion = 15,
    ForceWeatherOrage = 16,
    ForceWeatherAccalmie = 17,
    RerollWeather = 18,
    GrantTeamSkillPoints = 19,
    GrantTemporarySkill = 20
}
