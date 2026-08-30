namespace Leds.GameEngine.Domain.Nodes;

public enum NodeState
{
    Planned = 0,
    Available = 1,
    Selected = 2,
    Locked = 3,
    Resolved = 4,
    Unreachable = 5
}