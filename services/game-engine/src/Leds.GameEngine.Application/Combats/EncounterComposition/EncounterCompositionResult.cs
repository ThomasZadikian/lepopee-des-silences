using Leds.GameEngine.Application.Catalog;

namespace Leds.GameEngine.Application.Combats.EncounterComposition;

public sealed record EncounterCompositionResult(
    int DifficultyBudget,
    int EnemyCount,
    IReadOnlyCollection<CatalogEnemyDefinition> SelectedEnemies);
