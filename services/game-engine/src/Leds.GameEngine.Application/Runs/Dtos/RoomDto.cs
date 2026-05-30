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
    IReadOnlyCollection<NodeLayerDto> NodeLayers,
    IReadOnlyCollection<NodeDto> AvailableNodes)
{
    public static RoomDto FromDomain(Room room)
    {
        var nodeLayers = room.Nodes
            .GroupBy(node => node.NodeDepth)
            .OrderBy(group => group.Key)
            .Select(group => new NodeLayerDto(
                group.Key,
                group.Select(NodeDto.FromDomain).ToArray()))
            .ToArray();

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
            nodeLayers,
            room.AvailableNodes.Select(NodeDto.FromDomain).ToArray());
    }
}