using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

public sealed class DeterministicEncounterEnemySelector : IEncounterEnemySelector
{
    private static readonly IReadOnlyDictionary<int, int> MaxEnemyCountByRiskLevel = new Dictionary<int, int>
    {
        [1] = 1, [2] = 1, [3] = 2, [4] = 2, [5] = 3,
    };

    private readonly DeterministicWeightedSelector _weightedSelector;

    public DeterministicEncounterEnemySelector(DeterministicWeightedSelector weightedSelector)
    {
        _weightedSelector = weightedSelector;
    }

    public IReadOnlyCollection<SelectedEnemyDefinition> SelectEnemies(
        EncounterEnemySelectionContext context,
        IReadOnlyCollection<CatalogEnemyDefinitionSnapshot> candidates)
    {
        if (candidates.Count == 0)
            return [];

        var filtered = FilterByNodeType(candidates, context.NodeEventType);
        if (filtered.Count == 0)
            filtered = candidates.ToList();

        var maxCount = GetMaxEnemyCount(context.NodeEventType, context.RiskLevel);

        var selectionCandidates = filtered
            .Select(e => new SelectionCandidate(
                e.Key,
                e.EncounterWeight <= 0 ? 1 : e.EncounterWeight,
                e.Rank))
            .ToList();

        var selectionContext = new SelectionContext(
            context.RunId,
            context.RoomId,
            context.NodeId,
            context.Seed,
            "EnemySelection",
            $"{context.RoomType}:{context.NodeEventType}:{context.RiskLevel}");

        var result = _weightedSelector.Select(selectionContext, selectionCandidates, maxCount);

        var enemyLookup = filtered.ToDictionary(e => e.Key, StringComparer.OrdinalIgnoreCase);

        return result.Selected
            .Where(c => enemyLookup.ContainsKey(c.Key))
            .Select(c => new SelectedEnemyDefinition(
                enemyLookup[c.Key], context.DifficultyMultiplier))
            .ToList();
    }

    private static IReadOnlyList<CatalogEnemyDefinitionSnapshot> FilterByNodeType(
        IReadOnlyCollection<CatalogEnemyDefinitionSnapshot> candidates,
        string nodeEventType)
    {
        return nodeEventType switch
        {
            "Elite" => candidates.Where(e => e.IsElite || e.Rank == "Elite").ToList(),
            "RoomBoss" => candidates.Where(e => e.IsBoss || e.Rank == "Boss").ToList(),
            _ => candidates.Where(e => !e.IsBoss && !e.IsElite).ToList(),
        };
    }

    private static int GetMaxEnemyCount(string nodeEventType, int riskLevel)
    {
        return nodeEventType switch
        {
            "Elite" => 1,
            "RoomBoss" => 1,
            "Rare" => 1,
            _ => MaxEnemyCountByRiskLevel.GetValueOrDefault(Math.Clamp(riskLevel, 1, 5), 1),
        };
    }
}
