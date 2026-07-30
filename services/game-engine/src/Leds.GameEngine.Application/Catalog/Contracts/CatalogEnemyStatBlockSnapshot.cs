namespace Leds.GameEngine.Application.Catalog.Contracts;

public sealed record CatalogEnemyStatBlockSnapshot(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Movement = 4);
