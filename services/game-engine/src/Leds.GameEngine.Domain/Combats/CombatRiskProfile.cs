namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// Immutable value object representing the risk-scaling context of a combat encounter.
/// Produced by <c>ICombatRiskProfileResolver</c> and attached to <c>RewardOffer</c>
/// so that item-generation pipelines can scale reward power deterministically.
/// Driven entirely by the node's <see cref="RiskTier"/> — a single per-tier lookup
/// table, replacing the old raw-0-100-delta formula.
/// </summary>
/// <param name="Tier">Combat tier derived from the node event type (Normal/Rare/Elite/RoomBoss/FinalBoss).</param>
/// <param name="RiskTier">The node's combat danger tier (Calme..Fatal).</param>
/// <param name="DifficultyMultiplier">Base stat multiplier applied to every enemy in the encounter.</param>
/// <param name="LootMultiplier">Multiplies each loot entry's drop percent, capped at 100%.</param>
/// <param name="ReputationMultiplier">Multiplies reputation gain from resolving this combat.</param>
/// <param name="EclatsBaseAmount">Flat Éclats du Palais granted on combat resolution.</param>
public sealed record CombatRiskProfile(
    CombatTier Tier,
    RiskTier RiskTier,
    double DifficultyMultiplier,
    double LootMultiplier,
    double ReputationMultiplier,
    int EclatsBaseAmount);
