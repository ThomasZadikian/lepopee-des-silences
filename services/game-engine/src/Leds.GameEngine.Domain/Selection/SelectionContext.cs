namespace Leds.GameEngine.Domain.Selection;

public sealed record SelectionContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string Seed,
    string DecisionType,
    string? ContextKey,
    string AlgorithmVersion = "deterministic-v2");
