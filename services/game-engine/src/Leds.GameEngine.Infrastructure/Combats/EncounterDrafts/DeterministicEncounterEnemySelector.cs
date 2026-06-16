using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

public sealed class DeterministicEncounterEnemySelector : IEncounterEnemySelector
{
    private static readonly IReadOnlyDictionary<int, int> MaxEnemyCountByRiskLevel = new Dictionary<int, int>
    {
        [1] = 1, [2] = 1, [3] = 2, [4] = 2, [5] = 3,
    };

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
        var seedHash = ComputeSeedHash(context.Seed, context.RunId, context.NodeId);
        var rng = new Random(seedHash);

        var weighted = filtered
            .Select(e => (Enemy: e, Weight: Math.Max(1, e.EncounterWeight)))
            .ToList();

        var selected = new List<SelectedEnemyDefinition>();

        for (var i = 0; i < maxCount && weighted.Count > 0; i++)
        {
            var totalWeight = weighted.Sum(w => w.Weight);
            var roll = rng.Next(totalWeight);
            var cumulative = 0;
            var pickedIndex = 0;

            for (var j = 0; j < weighted.Count; j++)
            {
                cumulative += weighted[j].Weight;
                if (roll < cumulative)
                {
                    pickedIndex = j;
                    break;
                }
            }

            var picked = weighted[pickedIndex];
            selected.Add(new SelectedEnemyDefinition(
                picked.Enemy, context.DifficultyMultiplier));

            weighted.RemoveAt(pickedIndex);
        }

        return selected;
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

    private static int ComputeSeedHash(string seed, Guid runId, Guid nodeId)
    {
        unchecked
        {
            var hash = seed.GetHashCode();
            hash = hash * 397 ^ runId.GetHashCode();
            hash = hash * 397 ^ nodeId.GetHashCode();
            return hash;
        }
    }
}
