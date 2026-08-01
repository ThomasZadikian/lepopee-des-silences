namespace Leds.GameEngine.Domain.Rewards;

public enum RewardType
{
    Heal = 1,
    TemporaryItem = 2,
    StatBonus = 3,
    MemoryFragment = 4,
    /// <summary>A no-op choice — walks away from a reward offer (e.g. a merchant's
    /// "Refuser") without granting anything. Always affordable, always available,
    /// so the player is never forced into a purchase to progress.</summary>
    Decline = 5
}