using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

/// <summary>
/// <para>
/// "Groupes d'ennemis cohérents" (Chantier Bestiaire Phase 14) : quand un combat
/// tire plus d'un ennemi, une famille "ancre" est d'abord tirée (pondérée par le
/// poids total de ses membres éligibles) parmi les familles représentées 2+ fois
/// dans le pool, puis le poids de ses membres est multiplié pour le tirage
/// principal — les rencontres tendent à regrouper des créatures de la même
/// famille ("qui travaillent ensemble") plutôt qu'un pur tirage indépendant,
/// sans le garantir : les autres candidats restent tirables (cohérence
/// probabiliste, pas une composition fixe). Les "10 compositions de combat"
/// précises listées dans la SFD Bestiaire ne sont pas reproduites ici — le texte
/// source n'était pas disponible au moment de l'implémentation ; cette
/// approximation par famille capture l'esprit ("groupes récurrents") sans les
/// compositions exactes.
/// </para>
/// </summary>
public sealed class DeterministicEncounterEnemySelector : IEncounterEnemySelector
{
    // ⚠ CETTE TABLE N'EST PAS CELLE QUI TOURNE.
    // Ce sélecteur est enregistré au conteneur mais aucun code de production ne l'injecte :
    // la composition des rencontres passe par EncounterCompositionPolicy, appelée par
    // CombatEncounterDraftGenerator. Les effectifs réels y sont définis
    // (GetMaxEnemiesForEarlyRun + MaxEnemiesPerEncounter), et ils vont désormais jusqu'à 5.
    // Les valeurs ci-dessous n'ont pas suivi et ne doivent pas servir de référence.
    private static readonly IReadOnlyDictionary<int, int> MaxEnemyCountByRiskLevel = new Dictionary<int, int>
    {
        [1] = 1, [2] = 1, [3] = 2, [4] = 2, [5] = 3,
    };

    /// <summary>Weight multiplier applied to the anchor family's members so encounters cluster without guaranteeing it.</summary>
    private const int FamilyCohesionWeightMultiplier = 3;

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

        var selectionCandidates = BuildWeightedCandidates(context, filtered, maxCount);

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

    /// <summary>
    /// Base per-enemy weights, boosted for the members of one "anchor" family when
    /// more than one enemy will be drawn and at least one family has 2+ eligible
    /// members — see the class-level doc comment for the full rationale.
    /// </summary>
    private List<SelectionCandidate> BuildWeightedCandidates(
        EncounterEnemySelectionContext context,
        IReadOnlyList<CatalogEnemyDefinitionSnapshot> filtered,
        int maxCount)
    {
        var baseCandidates = filtered
            .Select(e => new SelectionCandidate(
                e.Key,
                e.EncounterWeight <= 0 ? 1 : e.EncounterWeight,
                e.Rank))
            .ToList();

        if (maxCount <= 1)
            return baseCandidates;

        var familyGroups = filtered
            .Where(e => !string.IsNullOrWhiteSpace(e.Family))
            .GroupBy(e => e.Family!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .ToList();

        if (familyGroups.Count == 0)
            return baseCandidates;

        var familyCandidates = familyGroups
            .Select(g => new SelectionCandidate(g.Key, g.Sum(e => Math.Max(1, e.EncounterWeight))))
            .ToList();

        // Distinct decision type/context from the main enemy draw below so the two
        // deterministic rolls don't share the same hash input.
        var familySelectionContext = new SelectionContext(
            context.RunId,
            context.RoomId,
            context.NodeId,
            context.Seed,
            "EnemyFamilyAnchor",
            $"{context.RoomType}:{context.NodeEventType}:{context.RiskLevel}");

        var anchorFamily = _weightedSelector.Select(familySelectionContext, familyCandidates, 1)
            .Selected.FirstOrDefault()?.Key;

        if (anchorFamily is null)
            return baseCandidates;

        var anchorMembers = familyGroups
            .First(g => string.Equals(g.Key, anchorFamily, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return baseCandidates
            .Select(c => anchorMembers.Contains(c.Key)
                ? new SelectionCandidate(c.Key, c.Weight * FamilyCohesionWeightMultiplier, c.SelectionGroup)
                : c)
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
