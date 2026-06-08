using Leds.GameEngine.Domain.Nodes;

namespace Leds.GameEngine.Application.RoomMaps;

/// <summary>
/// Associates a node event type with a relative selection weight.
/// Higher weights increase the probability of being chosen during generation.
/// </summary>
public sealed record NodeTypeWeight(NodeEventType NodeType, int Weight)
{
    public int Weight { get; init; } = Weight > 0
        ? Weight
        : throw new ArgumentException("Weight must be greater than zero.", nameof(Weight));
}
