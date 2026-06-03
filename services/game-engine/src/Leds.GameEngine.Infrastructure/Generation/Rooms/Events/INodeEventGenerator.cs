using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Generation.Rooms.Events;

public interface INodeEventGenerator
{
    IReadOnlyCollection<NodeEvent> Generate(
        Random random,
        string seed,
        string matrixVersion,
        RoomType roomType,
        int roomDepth,
        int nodeDepth,
        int totalNodeCount,
        IRoomEventGenerationState state);
}