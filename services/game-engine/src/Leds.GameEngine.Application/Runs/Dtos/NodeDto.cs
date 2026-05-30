using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record NodeDto(
    Guid Id,
    string EventType,
    int RiskLevel,
    string RewardProfile,
    string State)
{
    public static NodeDto FromDomain(Node node)
    {
        return new NodeDto(
            node.Id.Value,
            node.EventType.ToString(),
            node.RiskLevel,
            node.RewardProfile,
            node.State.ToString());
    }
}