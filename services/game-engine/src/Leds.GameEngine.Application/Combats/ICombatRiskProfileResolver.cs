using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Combats;

/// <summary>
/// Centralizes the risk-scaling formula for combat encounters.
/// Given a node event type and its actual risk level, produces a
/// <see cref="CombatRiskProfile"/> that carries the multipliers used by
/// both the combat difficulty system and the reward-power budget.
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
    /// <param name="actualRiskLevel">Risk level of the map node (0–100).</param>
    CombatRiskProfile Resolve(NodeEventType eventType, int actualRiskLevel);

    /// <summary>Returns true if the event type is handled by the combat risk system.</summary>
    bool IsCombatNodeType(NodeEventType eventType);
}