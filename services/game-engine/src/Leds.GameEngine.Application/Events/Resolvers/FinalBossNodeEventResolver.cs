using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class FinalBossNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.FinalBoss;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.FinalBossEncounterStarted,
            "Him’Lit",
            "La confrontation finale commence.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Cette fois, le silence te reconnaît.")
            });
    }
}