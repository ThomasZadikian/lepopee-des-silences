using Leds.GameEngine.Application.Catalog;

namespace Leds.GameEngine.Application.Combats.EncounterComposition;

public sealed record EncounterCompositionContext(
    string RoomType,
    int RoomIndex,
    int RiskLevel,
    string EncounterType,
    IReadOnlyCollection<CatalogEnemyDefinition> AvailableEnemies);
