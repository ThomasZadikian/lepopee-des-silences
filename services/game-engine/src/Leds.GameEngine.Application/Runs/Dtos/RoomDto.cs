using Leds.GameEngine.Domain.Rooms;

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
    string? LayoutTemplateVersion)
{
    public static RoomDto FromDomain(Room room)
    {
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
            room.LayoutTemplateVersion);
    }
}