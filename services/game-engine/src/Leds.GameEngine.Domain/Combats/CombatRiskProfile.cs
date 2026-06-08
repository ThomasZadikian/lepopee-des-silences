namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Immutable value object representing the risk-scaling context of a combat encounter.
/// Produced by <c>ICombatRiskProfileResolver</c> and attached to <c>RewardOffer</c>
/// so that item-generation pipelines can later scale reward power deterministically.
/// </summary>
/// <param name="Tier">Combat tier derived from the node event type.</param>
/// <param name="BaseRisk">Reference risk level for this tier (e.g. Elite = 35).</param>
/// <param name="ActualRisk">Actual risk level of the map node (0–100).</param>
/// <param name="RiskDelta">max(0, ActualRisk − BaseRisk) — never negative.</param>
/// <param name="DifficultyMultiplier">
/// clamp(1.0 + RiskDelta / 100, 1.0, 1.75).
/// Intended future use: scale enemy stats.
/// </param>
/// <param name="RewardPowerMultiplier">
/// Equals DifficultyMultiplier — scales the power budget of the generated reward.
/// </param>
/// <param name="RiskBand">Qualitative band of the actual risk level.</param>
public sealed record CombatRiskProfile(
    CombatTier Tier,
    int        BaseRisk,
    int        ActualRisk,
    int        RiskDelta,
    double     DifficultyMultiplier,
    double     RewardPowerMultiplier,
    RiskBand   RiskBand);
