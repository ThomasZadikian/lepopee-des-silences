using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Common;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public sealed class NodeEventCandidateResolver : INodeEventCandidateResolver
{
    public IReadOnlyList<NodeEventType> ResolveCandidates(
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState state,
        IReadOnlyCollection<NodeEvent> currentNodeEvents)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(currentNodeEvents);

        var candidates = new List<NodeEventType>
        {
            NodeEventType.Combat,
            NodeEventType.Rest,
            NodeEventType.Item,
            NodeEventType.Npc,
            NodeEventType.Merchant,
            NodeEventType.Law,
            NodeEventType.Curse,
            NodeEventType.Rare
        };

        if (currentNodeEvents.Count > 0)
        {
            candidates.Remove(NodeEventType.Rest);
        }

        if (state.EliteCount < RoomGenerationConstants.MaxElitePerRoom)
        {
            candidates.Add(NodeEventType.Elite);
        }

        if (state.NpcCount >= RoomGenerationConstants.MaxNpcPerRoom)
        {
            candidates.Remove(NodeEventType.Npc);
        }

        if (state.RestCount >= RoomGenerationConstants.MaxRestPerRoom)
        {
            candidates.Remove(NodeEventType.Rest);
        }

        if (state.MerchantCount >= RoomGenerationConstants.MaxMerchantPerRoom)
        {
            candidates.Remove(NodeEventType.Merchant);
        }

        if (state.LawCount >= RoomGenerationConstants.MaxLawPerRoom)
        {
            candidates.Remove(NodeEventType.Law);
        }

        if (state.CurseCount >= RoomGenerationConstants.MaxCursePerRoom)
        {
            candidates.Remove(NodeEventType.Curse);
        }

        if (state.RareCount >= RoomGenerationConstants.MaxRarePerRoom)
        {
            candidates.Remove(NodeEventType.Rare);
        }

        var maxItemCount = Math.Max(1, totalNodeCount / 2);

        if (state.ItemCount >= maxItemCount ||
            currentNodeEvents.Any(nodeEvent => nodeEvent.EventType == NodeEventType.Item))
        {
            candidates.Remove(NodeEventType.Item);
        }

        ApplyRoomTypeBias(roomType, candidates);

        if (candidates.Count == 0)
        {
            candidates.Add(NodeEventType.Combat);
        }

        return candidates;
    }

    private static void ApplyRoomTypeBias(RoomType roomType, List<NodeEventType> candidates)
    {
        switch (roomType)
        {
            case RoomType.Memory:
                candidates.Add(NodeEventType.Npc);
                candidates.Add(NodeEventType.Law);
                candidates.Add(NodeEventType.Rare);
                break;

            case RoomType.Forest:
                candidates.Add(NodeEventType.Rest);
                candidates.Add(NodeEventType.Merchant);
                break;

            case RoomType.Rupture:
                candidates.Add(NodeEventType.Combat);
                candidates.Add(NodeEventType.Elite);
                candidates.Add(NodeEventType.Curse);
                break;

            case RoomType.Silence:
                candidates.Add(NodeEventType.Law);
                candidates.Add(NodeEventType.Curse);
                break;

            case RoomType.Antechamber:
                candidates.Add(NodeEventType.Elite);
                candidates.Add(NodeEventType.Rare);
                break;

            case RoomType.Final:
                candidates.Clear();
                candidates.Add(NodeEventType.Combat);
                candidates.Add(NodeEventType.Elite);
                candidates.Add(NodeEventType.Curse);
                candidates.Add(NodeEventType.Rare);
                break;
        }
    }
}