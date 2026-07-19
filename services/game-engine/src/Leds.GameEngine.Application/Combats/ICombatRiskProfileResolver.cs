using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Centralizes the risk-scaling tables for combat encounters.
/// Given a node event type and its combat risk tier, produces a
/// <see cref="CombatRiskProfile"/> that carries the multipliers used by
/// the combat difficulty system and by loot/reputation/currency rewards.
/// </summary>
public interface ICombatRiskProfileResolver
{
    /// <summary>
    /// Resolves the combat risk profile for a combat node.
    /// </summary>
    /// <param name="eventType">
    /// Must be a combat type: Combat, Rare, Elite, RoomBoss, or FinalBoss.
    /// Throws <see cref="ArgumentException"/> for non-combat types.
    /// </param>
    /// <param name="riskLevel">
    /// The node's combat risk tier, as the 1-5 ordinal of <see cref="RiskTier"/>
    /// (Calme=1 .. Fatal=5). Kept as a plain int to match
    /// <c>EncounterCompositionContext.RiskLevel</c>, which already carries this same value.
    /// </param>
    CombatRiskProfile Resolve(NodeEventType eventType, int riskLevel);

    /// <summary>Returns true if the event type is handled by the combat risk system.</summary>
    bool IsCombatNodeType(NodeEventType eventType);
}