using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Planning;

public interface IRoomPlanGenerator
{
    Room Generate(
        string seed,
        string matrixVersion,
        int roomDepth,
        RoomType roomType,
        Random random);
}