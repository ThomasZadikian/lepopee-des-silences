using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Themes;

public sealed class RoomThemeResolver : IRoomThemeResolver
{
    public string Resolve(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Threshold => "Threshold",
            RoomType.Memory => "Memory",
            RoomType.Forest => "Forest",
            RoomType.Rupture => "Rupture",
            RoomType.Silence => "Silence",
            RoomType.Antechamber => "Antechamber",
            RoomType.Final => "Final",
            _ => "Unknown"
        };
    }
}