using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record NodeDto(
    Guid Id,
    IReadOnlyCollection<NodeEventDto> Events,
    IReadOnlyCollection<string> EventTypes,
    int EventCount,
    string? ResolvedEventType,
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
            node.Events
                .OrderBy(nodeEvent => nodeEvent.Order)
                .Select(NodeEventDto.FromDomain)
                .ToArray(),
            node.EventTypes
                .Select(eventType => eventType.ToString())
                .ToArray(),
            node.EventCount,
            node.ResolvedEvent?.EventType.ToString(),
            node.RiskLevel,
            node.RewardProfile,
            node.State.ToString(),
            node.NodeDepth,
            node.ParentNodeId?.Value,
            node.ParentNodeIds
                .Select(parentNodeId => parentNodeId.Value)
                .ToArray(),
            node.IsRoomBossNode,
            node.ChosenEventOptionId,
            node.HasChosenEventOption);
    }
}