namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Identifies the mechanical effect of a <see cref="RunModifier"/>.
/// </summary>
public enum RunModifierType
{
    /// <summary>
    /// Adds the modifier's <c>Value</c> as starting guard to the player combatant
    /// at the beginning of every combat. Not consumed by hits.
    /// </summary>
    StartingGuardBonus = 0,

    /// <summary>
    /// Multiplies the enemy encounter difficulty for the next combat by
    /// <c>1 + Value</c> (e.g. 0.10 → +10% difficulty). Consumed after that combat.
    /// </summary>
    NextCombatDifficultyMultiplier = 1,

    /// <summary>
    /// Multiplies the reward power granted after combat by <c>1 + Value</c>.
    /// Duration determines when it is consumed.
    /// </summary>
    RewardPowerMultiplierBonus = 2,

    /// <summary>
    /// Permanently increases combat difficulty for the entire run.
    /// Applied by Palace Laws targeting the Combat domain.
    /// Stacks additively across laws. Value is a delta (0.10 = +10%).
    /// Unlike <see cref="NextCombatDifficultyMultiplier"/>, this is never consumed.
    /// </summary>
    PermanentCombatDifficultyBonus = 3,
}
