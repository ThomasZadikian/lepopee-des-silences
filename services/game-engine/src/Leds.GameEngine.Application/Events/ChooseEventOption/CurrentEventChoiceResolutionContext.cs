using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed record CurrentEventChoiceResolutionContext(
    Run Run,
    Room Room,
    Node Node,
    string ChoiceId);