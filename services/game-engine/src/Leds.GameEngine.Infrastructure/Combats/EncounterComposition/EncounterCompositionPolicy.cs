using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterComposition;

public sealed class EncounterCompositionPolicy : IEncounterCompositionPolicy
{
    private static readonly IReadOnlyDictionary<int, int> BaseBudgetByRiskLevel = new Dictionary<int, int>
    {
        [1] = 2,
        [2] = 3,
        [3] = 4,
        [4] = 5,
        [5] = 7,
    };

    private static readonly IReadOnlyDictionary<string, int> ArchetypeCosts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Fragile"] = 1,
        ["Support"] = 1,
        ["Skirmisher"] = 2,
        ["Guard"] = 2,
        ["Disruptor"] = 2,
        ["Bruiser"] = 3,
        ["Elite"] = 4,
    };

    private const int UnknownArchetypeCost = 2;

    public EncounterCompositionResult Compose(EncounterCompositionContext context)
    {
        if (context.AvailableEnemies.Count == 0)
        {
            throw new DomainException("No compatible enemy definitions were found for encounter composition.");
        }

        if (context.RiskLevel < 1 || context.RiskLevel > 5)
        {
            throw new DomainException($"Invalid risk level: {context.RiskLevel}. Risk level must be between 1 and 5.");
        }

        var knownTypes = new[] { "Combat", "Elite", "Rare", "RoomBoss" };
        if (!knownTypes.Contains(context.EncounterType, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException($"Unknown encounter type: {context.EncounterType}.");
        }

        var budget = CalculateBudget(context);
        var eligible = FilterEligibleEnemies(context);

        if (eligible.Count == 0)
        {
            throw new DomainException("No compatible enemy definitions were found for encounter composition.");
        }

        var selected = context.EncounterType switch
        {
            "Combat" => SelectCombatEnemies(eligible, budget),
            "Elite" => SelectEliteEnemies(eligible, budget),
            "Rare" => SelectRareEnemies(eligible, budget),
            "RoomBoss" => SelectRoomBossEnemies(eligible),
            _ => SelectCombatEnemies(eligible, budget),
        };

        return new EncounterCompositionResult(
            DifficultyBudget: budget,
            EnemyCount: selected.Count,
            SelectedEnemies: selected);
    }

    private static int CalculateBudget(EncounterCompositionContext context)
    {
        var budget = BaseBudgetByRiskLevel.GetValueOrDefault(context.RiskLevel, 4);

        budget += context.EncounterType switch
        {
            "Elite" => 2,
            "Rare" => 1,
            _ => 0,
        };

        if (context.RoomIndex >= 6)
        {
            budget += 2;
        }
        else if (context.RoomIndex >= 3)
        {
            budget += 1;
        }

        return budget;
    }

    private static List<CatalogEnemyDefinition> FilterEligibleEnemies(EncounterCompositionContext context)
    {
        return context.AvailableEnemies
            .Where(e => e.MinRiskLevel <= context.RiskLevel && context.RiskLevel <= e.MaxRiskLevel)
            .OrderBy(e => GetArchetypeCost(e.Archetype))
            .ThenByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectCombatEnemies(
        List<CatalogEnemyDefinition> eligible, int budget)
    {
        const int maxEnemies = 3;
        var selected = new List<CatalogEnemyDefinition>();
        var remainingBudget = budget;

        foreach (var enemy in eligible)
        {
            if (selected.Count >= maxEnemies)
            {
                break;
            }

            var cost = GetArchetypeCost(enemy.Archetype);
            if (cost <= remainingBudget)
            {
                selected.Add(enemy);
                remainingBudget -= cost;
            }
        }

        if (selected.Count == 0)
        {
            selected.Add(eligible[0]);
        }

        return selected;
    }

    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectEliteEnemies(
        List<CatalogEnemyDefinition> eligible, int budget)
    {
        var preferred = eligible
            .FirstOrDefault(e => e.Tags.Contains("elite", StringComparer.OrdinalIgnoreCase)
                              || string.Equals(e.Archetype, "Elite", StringComparison.OrdinalIgnoreCase));

        if (preferred is not null)
        {
            var cost = GetArchetypeCost(preferred.Archetype);
            var remaining = budget - cost;

            var second = eligible
                .Where(e => e.Key != preferred.Key)
                .FirstOrDefault(e => GetArchetypeCost(e.Archetype) <= remaining);

            return second is not null
                ? new[] { preferred, second }
                : new[] { preferred };
        }

        var hardest = eligible
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .First();

        return [hardest];
    }

    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectRareEnemies(
        List<CatalogEnemyDefinition> eligible, int budget)
    {
        var preferred = eligible
            .FirstOrDefault(e =>
                string.Equals(e.Archetype, "Support", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.Archetype, "Disruptor", StringComparison.OrdinalIgnoreCase));

        if (preferred is not null)
        {
            var cost = GetArchetypeCost(preferred.Archetype);
            var remaining = budget - cost;

            var second = eligible
                .Where(e => e.Key != preferred.Key)
                .FirstOrDefault(e => GetArchetypeCost(e.Archetype) <= remaining);

            return second is not null
                ? new[] { preferred, second }
                : new[] { preferred };
        }

        var hardest = eligible
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .First();

        return [hardest];
    }

    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectRoomBossEnemies(
        List<CatalogEnemyDefinition> eligible)
    {
        var boss = eligible
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .First();

        return [boss];
    }

    private static int GetArchetypeCost(string archetype)
    {
        return ArchetypeCosts.GetValueOrDefault(archetype, UnknownArchetypeCost);
    }
}
