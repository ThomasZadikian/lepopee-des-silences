using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public interface INodeEventCandidateResolver
{
    IReadOnlyList<NodeEventType> ResolveCandidates(
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState state,
        IReadOnlyCollection<NodeEvent> currentNodeEvents);
}