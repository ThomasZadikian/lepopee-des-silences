using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Runs.AdvanceRoomActors;

public sealed record ActorMovementDto(
    Guid ActorId,
    string ActorKind,
    int FromX,
    int FromY,
    int ToX,
    int ToY);

public sealed record AdvanceRoomActorsResponse(
    RunDto Run,
    IReadOnlyCollection<ActorMovementDto> Movements,
    Guid? TriggeredNodeId);
