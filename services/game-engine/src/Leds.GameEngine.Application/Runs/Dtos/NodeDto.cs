using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record NodeDto(
    Guid Id,
    IReadOnlyCollection<string> EventTypes,
    int EventCount,
    int RiskLevel,
    string RewardProfile,
    string State,
    int NodeDepth,
    Guid? ParentNodeId,
    IReadOnlyCollection<Guid> ParentNodeIds,
    bool IsRoomBossNode,
    string? ChosenEventOptionId,
    bool HasChosenEventOption)
{
    public static NodeDto FromDomain(Node node)
    {
        return new NodeDto(
            node.Id.Value,
            node.EventTypes.Select(eventType => eventType.ToString()).ToArray(),
            node.EventCount,
            node.RiskLevel,
            node.RewardProfile,
            node.State.ToString(),
            node.NodeDepth,
            node.ParentNodeId?.Value,
            node.ParentNodeIds.Select(parentNodeId => parentNodeId.Value).ToArray(),
            node.IsRoomBossNode,
            node.ChosenEventOptionId,
            node.HasChosenEventOption);
    }
}