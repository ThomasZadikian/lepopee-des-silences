using Leds.GameEngine.Application.Catalog.Contracts;

namespace Leds.GameEngine.Application.Combats.EncounterDrafts;

public interface IEncounterEnemySelector
{
    IReadOnlyCollection<SelectedEnemyDefinition> SelectEnemies(
        EncounterEnemySelectionContext context,
        IReadOnlyCollection<CatalogEnemyDefinitionSnapshot> candidates);
}

public sealed record EncounterEnemySelectionContext(
    Guid RunId,
    Guid RoomId,
    Guid NodeId,
    string Seed,
    string RoomType,
    string? RoomDefinitionKey,
    int RoomDepth,
    string NodeEventType,
    int RiskLevel,
    decimal DifficultyMultiplier,
    IReadOnlyCollection<string> ActiveLawKeys,
    IReadOnlyCollection<string> ActiveCurseKeys);

public sealed record SelectedEnemyDefinition(
    CatalogEnemyDefinitionSnapshot Enemy,
    decimal DifficultyMultiplier);
