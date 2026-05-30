using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RoomDto(
    Guid Id,
    int Depth,
    string Theme,
    string State,
    int CurrentNodeDepth,
    int MaxNodeDepth,
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
            room.Theme,
            room.State.ToString(),
            room.CurrentNodeDepth,
            room.MaxNodeDepth,
            nodeLayers,
            room.AvailableNodes.Select(NodeDto.FromDomain).ToArray());
    }
}