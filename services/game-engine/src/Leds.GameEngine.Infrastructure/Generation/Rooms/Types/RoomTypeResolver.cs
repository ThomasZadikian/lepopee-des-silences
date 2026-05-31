using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

public sealed class RoomTypeResolver : IRoomTypeResolver
{
    public RoomType Resolve(int roomDepth, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (roomDepth == 0)
        {
            return RoomType.Threshold;
        }

        if (roomDepth >= 10)
        {
            return RoomType.Final;
        }

        var roomTypes = new[]
        {
            RoomType.Memory,
            RoomType.Forest,
            RoomType.Rupture,
            RoomType.Silence,
            RoomType.Antechamber
        };

        return roomTypes[random.Next(roomTypes.Length)];
    }
}