using Leds.GameEngine.Domain.Interlude;

namespace Leds.GameEngine.Application.Interlude.Dtos;

/// <summary>
/// DTO representation of a single interlude hub node.
/// Contains no riskLevel, rewardProfile, parentNodeIds, row/lane, or MapNode state.
/// </summary>
public sealed record InterludeNodeDto(
    Guid Id,
    string Type,
    string Label,
    string Description,
    string PositionSlot,
    bool IsEnabled,
    string ActionKey)
{
    public static InterludeNodeDto FromDomain(InterludeNode node) =>
        new(
            node.Id,
            node.Type.ToString(),
            node.Label,
            node.Description,
            node.PositionSlot,
            node.IsEnabled,
            node.ActionKey.ToString());
}
