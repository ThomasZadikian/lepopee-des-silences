using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterComposition;

public sealed class EncounterCompositionPolicy : IEncounterCompositionPolicy
{
    /// <summary>
    /// Nombre maximum d'ennemis engagés simultanément (SFD v2, §5). Passé de 4 à 5 pour que le
    /// palier Fatal puisse mettre une équipe de quatre en infériorité numérique.
    /// </summary>
    /// <remarks>
    /// Plafond commun aux deux systèmes de combat. La SFD envisageait de le rendre dépendant du
    /// mode — l'ATB restant à un chiffre plus bas — mais ce plafond est appliqué à la composition
    /// de la rencontre, en amont du combat, donc avant que le mode n'entre en jeu. Le rendre
    /// dépendant du moteur obligerait à faire descendre ce détail jusque dans la
    /// génération de brouillon, pour une différence d'un ennemi au seul palier Fatal.
    /// // BALANCE KNOB
    /// </remarks>
    private const int MaxEnemiesPerEncounter = 5;

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

    private static readonly IReadOnlyDictionary<PalaceRoomState, string[]> ArchetypePreferenceByState =
        new Dictionary<PalaceRoomState, string[]>
        {
            [PalaceRoomState.Silent] = ["Guard", "Support"],
            [PalaceRoomState.Painful] = ["Disruptor"],
            [PalaceRoomState.Enraged] = ["Bruiser", "Skirmisher"],
            [PalaceRoomState.Violent] = ["Bruiser", "Fragile"],
        };

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

        var knownTypes = new[] { "Combat", "Elite", "Rare", "RoomBoss", "FinalBoss" };
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

        string? preferredEnemyKey = null;

        var selected = context.EncounterType switch
        {
            "Combat" => SelectCombatEnemies(eligible, budget, context),
            "Elite" => SelectEliteEnemies(eligible, budget, context, out preferredEnemyKey),
            "Rare" => SelectRareEnemies(eligible, out preferredEnemyKey),
            "RoomBoss" => SelectRoomBossEnemies(eligible),
            "FinalBoss" => SelectRoomBossEnemies(eligible),
            _ => SelectCombatEnemies(eligible, budget, context),
        };

        // Plafond dur : jamais plus de MaxEnemiesPerEncounter combattants adverses à la fois.
        if (selected.Count > MaxEnemiesPerEncounter)
        {
            selected = selected.Take(MaxEnemiesPerEncounter).ToList();
        }

        return new EncounterCompositionResult(
            DifficultyBudget: budget,
            EnemyCount: selected.Count,
            SelectedEnemies: selected,
            PreferredEnemyKey: preferredEnemyKey);
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

        // Depth/RoomIndex is no longer a difficulty axis — risk tier alone drives budget now.

        // Palace Laws targeting Generation add +1 budget per active law, capped at +3.
        // This makes encounters progressively harder as more laws accumulate.
        if (context.ActivePalaceLaws is { Count: > 0 })
        {
            var generationLaws = context.ActivePalaceLaws
                .Count(law => law.Domains.Contains(PalaceLawDomain.Generation));

            budget += Math.Min(generationLaws, 3);
        }

        return budget;
    }

    private static List<CatalogEnemyDefinition> FilterEligibleEnemies(EncounterCompositionContext context)
    {
        var preferredArchetypes = ArchetypePreferenceByState.GetValueOrDefault(
            context.PalaceRoomState, []);

        return context.AvailableEnemies
            .Where(e => e.MinRiskLevel <= context.RiskLevel && context.RiskLevel <= e.MaxRiskLevel)
            .Where(e => IsBoundRoomCompatible(e, context.RoomKey))
            // A creature precisely bound to this room (Bestiaire) is that room's resident and
            // must win the pick over unrestricted/legacy filler competing for the same slot —
            // otherwise an unbound enemy with a cheaper archetype or an earlier Key deterministically
            // (see the "Determinism" tests below) shadows it forever, in every room, for every run.
            .OrderBy(e => IsPreciselyBoundToRoom(e, context.RoomKey) ? 0 : 1)
            .ThenBy(e => GetArchetypeCost(e.Archetype))
            .ThenBy(e => preferredArchetypes.Contains(e.Archetype, StringComparer.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .ToList();
    }

    // A Bestiaire creature bound to specific rooms (BoundRoomKeys non-empty) is only
    // eligible in one of those precise rooms — additive to, and stricter than, the
    // coarse RoomType match already applied upstream (ListCompatibleEnemyDefinitionsAsync).
    // A creature with no BoundRoomKeys is unrestricted (mirrors NpcEncounterSelector's
    // IsBoundRoomCompatible for NPCs).
    private static bool IsBoundRoomCompatible(CatalogEnemyDefinition enemy, string? roomKey)
    {
        if (enemy.BoundRoomKeys is not { Count: > 0 })
            return true;

        return roomKey is not null && enemy.BoundRoomKeys.Contains(roomKey, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsPreciselyBoundToRoom(CatalogEnemyDefinition enemy, string? roomKey)
    {
        return enemy.BoundRoomKeys is { Count: > 0 }
            && roomKey is not null
            && enemy.BoundRoomKeys.Contains(roomKey, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectCombatEnemies(
        List<CatalogEnemyDefinition> eligible, int budget, EncounterCompositionContext context)
    {
        // Early-run balance: limit enemy count at low depth/risk
        var maxEnemies = GetMaxEnemiesForEarlyRun(context);

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

    /// <summary>
    /// Limits enemy count based purely on risk tier — depth no longer plays into this
    /// (risk tier is the sole difficulty axis; depth is now purely structural).
    /// </summary>
    private static int GetMaxEnemiesForEarlyRun(EncounterCompositionContext context)
    {
        // Risque faible (Calme/Tendu) : 2 ennemis au plus
        if (context.RiskLevel <= 2)
            return 2;

        // Risque moyen (Dangereux) : 3
        if (context.RiskLevel <= 3)
            return 3;

        // Risque élevé (Périlleux) : 4
        if (context.RiskLevel <= 4)
            return 4;

        // Fatal : le plafond plein. C'est le seul palier qui peut submerger une équipe de
        // quatre en nombre — la contrepartie du pari que représente « provoquer le destin ».
        return MaxEnemiesPerEncounter;
    }

    // Elite = one strong "preferred" enemy (gets a stat bonus applied later, see
    // EnemyStatScaler/CombatEncounterDraftGenerator) optionally escorted by a STRICTLY
    // weaker enemy. Unlike the old logic, the escort is never allowed to be an equal or
    // stronger pick — if no strictly-weaker candidate fits the budget, the Elite fields alone.
    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectEliteEnemies(
        List<CatalogEnemyDefinition> eligible, int budget, EncounterCompositionContext context,
        out string? preferredEnemyKey)
    {
        var preferred = eligible
            .FirstOrDefault(e => e.Tags.Contains("elite", StringComparer.OrdinalIgnoreCase)
                              || string.Equals(e.Archetype, "Elite", StringComparison.OrdinalIgnoreCase));

        preferred ??= eligible
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .First();

        preferredEnemyKey = preferred.Key;

        var remaining = budget - GetArchetypeCost(preferred.Archetype);

        // L'escorte suit le palier de risque, comme une rencontre ordinaire : l'Elite occupe une
        // place, le reste de l'effectif autorisé peut l'accompagner. Elle restait auparavant
        // bornée à un seul accompagnateur, ce qui rendait un nœud Elite systématiquement moins
        // fourni qu'un combat normal de même palier — d'autant plus depuis que le plafond est
        // monté à MaxEnemiesPerEncounter.
        var maxEscorts = Math.Max(0, GetMaxEnemiesForEarlyRun(context) - 1);

        var squad = new List<CatalogEnemyDefinition> { preferred };

        // Strictement plus faible que l'Elite : l'escorte ne doit jamais lui voler la vedette,
        // ni transformer le nœud en combat à deux menaces équivalentes.
        var candidates = eligible
            .Where(e => e.Key != preferred.Key && e.BaseDifficulty < preferred.BaseDifficulty)
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (squad.Count - 1 >= maxEscorts)
                break;

            var candidateCost = GetArchetypeCost(candidate.Archetype);
            if (candidateCost > remaining)
                continue;

            squad.Add(candidate);
            remaining -= candidateCost;
        }

        return squad;
    }

    // Rare = always exactly one enemy, keyed off the catalog's Rarity field (not the
    // Support/Disruptor archetype heuristic this used to use, which had nothing to do
    // with rarity). Bridge fallback: if the catalog hasn't tagged any eligible enemy as
    // Rare yet (seeding follow-up), fall back to the old archetype heuristic so Rare
    // nodes don't come up empty in the meantime.
    // TODO: remove the archetype fallback once catalog Rarity tagging is complete.
    private static IReadOnlyCollection<CatalogEnemyDefinition> SelectRareEnemies(
        List<CatalogEnemyDefinition> eligible, out string? preferredEnemyKey)
    {
        var preferred = eligible
            .FirstOrDefault(e => string.Equals(e.Rarity, "Rare", StringComparison.OrdinalIgnoreCase));

        preferred ??= eligible
            .FirstOrDefault(e =>
                string.Equals(e.Archetype, "Support", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.Archetype, "Disruptor", StringComparison.OrdinalIgnoreCase));

        preferred ??= eligible
            .OrderByDescending(e => e.BaseDifficulty)
            .ThenBy(e => e.Key, StringComparer.Ordinal)
            .First();

        preferredEnemyKey = preferred.Key;

        return [preferred];
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
        var canonical = EnemyArchetypeCode.ParseRequired(archetype, "Enemy archetype");
        return ArchetypeCosts.TryGetValue(canonical, out var cost)
            ? cost
            : canonical switch
            {
                "Boss" => 4,
                "Tank" => 3,
                "Beast" or "Rupture" or "Shadow" or "Memory" or "Trauma" => 2,
                _ => throw new DomainException($"Enemy archetype '{canonical}' has no encounter cost.")
            };
    }
}
