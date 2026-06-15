using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record MapNodeDto(
    Guid Id,
    string Type,
    int Row,
    int Lane,
    int RiskLevel,
    string RewardProfile,
    IReadOnlyCollection<Guid> ParentNodeIds,
    string State,
    bool IsBoss,
    bool IsInitial,
    bool HasChosenEventOption)
{
    public static MapNodeDto FromDomain(MapNode node)
    {
        return new MapNodeDto(
            node.Id.Value,
            node.EventType.ToString(),
            node.Row,
            node.Lane,
            node.RiskLevel,
            node.RewardProfile,
            node.ParentNodeIds
                .Select(parentId => parentId.Value)
                .ToArray(),
            node.State.ToString(),
            node.IsBoss,
            node.IsInitial,
            node.HasChosenEventOption);
    }
}