using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Bosses;

public sealed class RoomBossProfileResolver : IRoomBossProfileResolver
{
    private readonly ICatalogContentGateway _catalogGateway;

    public RoomBossProfileResolver(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<RoomBossProfile> ResolveAsync(
        RoomType roomType,
        CancellationToken cancellationToken = default)
    {
        var catalogProfile = await _catalogGateway.GetRoomBossProfileAsync(
            roomType.ToString(),
            cancellationToken);

        if (catalogProfile is null)
        {
            throw new DomainException(
                $"No boss profile found for room type '{roomType}'. "
                + "Ensure the Catalog gateway has a profile registered for this room type.");
        }

        return RoomBossProfile.Create(
            catalogProfile.Key,
            catalogProfile.DisplayName,
            roomType,
            MapDifficultyToDangerHint(catalogProfile.BaseDifficulty),
            $"{catalogProfile.Key}-v1");
    }

    // BaseDifficulty is seeded as a small integer (CatalogSeedRunner.UpsertBossAsync passes
    // 2-5 across the five canon bosses), not the 0-100 scale this used to bucket against —
    // which flattened every boss, including the final boss, to "Low" regardless of severity.
    private static string MapDifficultyToDangerHint(int difficulty)
    {
        return difficulty switch
        {
            <= 1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Very High",
            _ => "Extreme"
        };
    }
}