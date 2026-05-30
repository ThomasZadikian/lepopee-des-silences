using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RoomDto(
    Guid Id,
    int Depth,
    string Theme,
    IReadOnlyCollection<NodeDto> Nodes)
{
    public static RoomDto FromDomain(Room room)
    {
        return new RoomDto(
            room.Id.Value,
            room.Depth,
            room.Theme,
            room.Nodes.Select(NodeDto.FromDomain).ToArray());
    }
}