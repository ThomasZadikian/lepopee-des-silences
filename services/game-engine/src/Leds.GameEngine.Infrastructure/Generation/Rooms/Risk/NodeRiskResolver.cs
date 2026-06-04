using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Risk;

public sealed class NodeRiskResolver : INodeRiskResolver
{
    public int Resolve(IReadOnlyCollection<NodeEvent> events, Random random)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(random);

        var baseRisk = events.Sum(nodeEvent => nodeEvent.EventType switch
        {
            NodeEventType.Rest => 5,
            NodeEventType.Memory => 10,
            NodeEventType.Item => 15,
            NodeEventType.Npc => 10,
            NodeEventType.Merchant => 5,
            NodeEventType.Law => 25,
            NodeEventType.Curse => 45,
            NodeEventType.Rare => 25,
            NodeEventType.Elite => 60,
            NodeEventType.RoomBoss => 85,
            NodeEventType.FinalBoss => 100,
            NodeEventType.Combat => 35,
            _ => 20
        });

        return Math.Clamp(baseRisk + random.Next(-5, 6), 0, 100);
    }
}