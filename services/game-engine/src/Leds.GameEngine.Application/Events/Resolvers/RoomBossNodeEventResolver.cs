using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.Events.Resolvers;

public sealed class RoomBossNodeEventResolver : INodeEventResolver
{
    public NodeEventType EventType => NodeEventType.RoomBoss;

    public NodeEventResolutionResult Resolve(NodeEventResolutionContext context)
    {
        var boss = context.Room.BossProfile
            ?? throw new DomainException("A boss encounter requires an authored boss profile.");

        return NodeEventResolutionResult.Create(
            NodeEventResolutionKind.RoomBossEncounterStarted,
            boss.Name,
            $"Le boss de la pièce se manifeste : {boss.Name}.",
            narrativeFragments: new[]
            {
                new NarrativeFragmentDto(
                    "Elise",
                    "Chaque pièce finit toujours par révéler ce qu’elle protégeait.")
            });
    }
}
