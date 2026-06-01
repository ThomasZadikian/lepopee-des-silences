using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class RoomBossNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.RoomBoss;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.RoomBossEncounterStarted,
            context.Room.BossProfile.Name,
            $"Le boss de la pièce se manifeste : {context.Room.BossProfile.Name}.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Chaque pièce finit toujours par révéler ce qu’elle protégeait.")
            });
    }
}