using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.States;

public interface IPalaceRoomStateResolver
{
    PalaceRoomState ResolveNextState(PalaceRoomStateResolutionContext context);
}

public sealed record PalaceRoomStateResolutionContext(
    string Seed,
    string MatrixVersion,
    PalaceRoomState PreviousRoomState,
    RoomType PreviousRoomType,
    RoomType NextRoomType,
    int NextRoomDepth,
    IReadOnlyCollection<string> ActiveLawKeys,
    IReadOnlyCollection<string> ActiveCurseKeys,
    string? ActiveClimate);
