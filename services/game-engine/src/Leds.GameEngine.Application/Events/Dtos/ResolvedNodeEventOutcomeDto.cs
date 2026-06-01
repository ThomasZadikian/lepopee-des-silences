using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Dtos;

public sealed record ResolvedNodeEventOutcomeDto(
    Guid NodeId,
    IReadOnlyCollection<string> EventTypes,
    string PrimaryEventType,
    string ResolutionKind,
    int RiskLevel,
    string RewardProfile,
    string Title,
    string Description,
    bool RequiresPlayerChoice,
    IReadOnlyCollection<NodeEventChoiceDto> Choices,
    IReadOnlyCollection<NarrativeFragmentDto> NarrativeFragments)
{
    public static ResolvedNodeEventOutcomeDto FromResult(
        Node node,
        NodeEventResolutionResult result)
    {
        return new ResolvedNodeEventOutcomeDto(
            node.Id.Value,
            node.EventTypes.Select(eventType => eventType.ToString()).ToArray(),
            node.EventType.ToString(),
            result.ResolutionKind.ToString(),
            node.RiskLevel,
            node.RewardProfile,
            result.Title,
            result.Description,
            result.RequiresPlayerChoice,
            result.Choices,
            result.NarrativeFragments);
    }
}