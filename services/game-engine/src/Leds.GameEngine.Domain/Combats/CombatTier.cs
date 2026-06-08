namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Classifies the tier of a combat encounter.
/// Used to look up the BaseRisk and derive the DifficultyMultiplier.
/// </summary>
public enum CombatTier
{
    Normal    = 0,
    Rare      = 1,
    Elite     = 2,
    RoomBoss  = 3,
    FinalBoss = 4
}
