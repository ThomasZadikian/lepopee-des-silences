using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ResolveNodeEvent;

public sealed record NodeEventResolutionContext(
    Run Run,
    Room Room,
    MapNode Node);