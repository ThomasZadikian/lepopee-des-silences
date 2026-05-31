using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public interface INodeEventGenerator
{
    IReadOnlyCollection<NodeEvent> Generate(
        Random random,
        RoomType roomType,
        int totalNodeCount,
        IRoomEventGenerationState state);
}