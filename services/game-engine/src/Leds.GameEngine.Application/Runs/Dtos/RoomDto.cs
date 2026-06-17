using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RoomDto(
    Guid Id,
    int Depth,
    string RoomType,
    string Theme,
    string State,
    int CurrentNodeDepth,
    int MaxNodeDepth,
    int TotalNodeCount,
    RoomBossProfileDto BossPreview,
    IReadOnlyCollection<MapNodeDto> Nodes,
    IReadOnlyCollection<MapNodeDto> AvailableNodes,
    string? LayoutTemplateKey,
    string? LayoutTemplateVersion,
    string? ActiveClimate = null)
{
    public static RoomDto FromDomain(Room room, IReadOnlyCollection<RunModifier>? runModifiers = null)
    {
        var activeClimate = ResolveActiveClimate(room, runModifiers ?? []);

        return new RoomDto(
            room.Id.Value,
            room.Depth,
            room.RoomType.ToString(),
            room.Theme,
            room.State.ToString(),
            room.CurrentNodeDepth,
            room.MaxNodeDepth,
            room.TotalNodeCount,
            RoomBossProfileDto.FromDomain(room.BossProfile),
            room.Nodes.Select(MapNodeDto.FromDomain).ToArray(),
            room.AvailableNodes.Select(MapNodeDto.FromDomain).ToArray(),
            room.LayoutTemplateKey,
            room.LayoutTemplateVersion,
            activeClimate);
    }

    private static string? ResolveActiveClimate(
        Room room,
        IReadOnlyCollection<RunModifier> runModifiers)
    {
        var modifier = runModifiers
            .Where(modifier =>
                modifier.Type == RunModifierType.RoomClimate &&
                !modifier.IsConsumed &&
                modifier.ExpiresAtRoomId == room.Id.Value)
            .OrderByDescending(modifier => modifier.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => "Grey",
            2 => "Rain",
            3 => "Heatwave",
            4 => "Hail",
            _ => null
        };
    }
}
