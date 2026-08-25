using Leds.GameEngine.Domain.Actors;
using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Domain.Rooms;

public enum ActorAdvanceMode
{
    All = 0,
    HostilesOnly = 1,
}

public sealed record ActorMovement(
    Guid ActorId,
    ActorKind ActorKind,
    int FromX,
    int FromY,
    int ToX,
    int ToY);

public sealed record ActorAdvanceResult(
    IReadOnlyCollection<ActorMovement> Movements,
    NodeId? TriggeredNodeId);
