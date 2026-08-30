namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record RoomEnemyPoolEligibilityContext(
    string RoomType,
    string? RoomDefinitionKey,
    int RoomDepth,
    string NodeEventType,
    int RiskLevel);
