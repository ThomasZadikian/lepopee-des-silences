using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Provides a <see cref="RoomTypeGenerationProfile"/> for a given <see cref="RoomType"/>.
/// </summary>
public interface IRoomTypeGenerationProfileProvider
{
    RoomTypeGenerationProfile GetProfile(RoomType roomType);
}
