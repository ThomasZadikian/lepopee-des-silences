using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RoomDto(
    Guid Id,
    int Depth,
    string Theme,
    string State,
    int CurrentNodeDepth,
    int MaxNodeDepth,
    IReadOnlyCollection<NodeDto> Nodes,
    IReadOnlyCollection<NodeDto> AvailableNodes)
{
    public static RoomDto FromDomain(Room room)
    {
        return new RoomDto(
            room.Id.Value,
            room.Depth,
            room.Theme,
            room.State.ToString(),
            room.CurrentNodeDepth,
            room.MaxNodeDepth,
            room.Nodes.Select(NodeDto.FromDomain).ToArray(),
            room.AvailableNodes.Select(NodeDto.FromDomain).ToArray());
    }
}