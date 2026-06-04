using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Rewards;

public sealed class NodeRewardProfileResolver : INodeRewardProfileResolver
{
    public string Resolve(IReadOnlyCollection<NodeEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventTypes = events
            .Select(nodeEvent => nodeEvent.EventType)
            .ToArray();

        if (eventTypes.Contains(NodeEventType.RoomBoss))
        {
            return "room-boss";
        }

        if (eventTypes.Contains(NodeEventType.Rare))
        {
            return "rare";
        }

        if (eventTypes.Contains(NodeEventType.Merchant))
        {
            return "trade";
        }

        if (eventTypes.Contains(NodeEventType.Rest) && eventTypes.Length == 1)
        {
            return "healing-only";
        }

        if (eventTypes.Contains(NodeEventType.Combat) ||
            eventTypes.Contains(NodeEventType.Elite))
        {
            return "combat-common";
        }

        if (eventTypes.Contains(NodeEventType.Law))
        {
            return "law-modifier";
        }

        if (eventTypes.Contains(NodeEventType.Curse))
        {
            return "high-risk";
        }

        if (eventTypes.Contains(NodeEventType.Npc))
        {
            return "narrative-choice";
        }

        return "common";
    }
}