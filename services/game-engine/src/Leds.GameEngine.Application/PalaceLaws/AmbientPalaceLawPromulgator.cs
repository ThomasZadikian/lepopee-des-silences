using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Selection;

namespace Leds.GameEngine.Application.PalaceLaws;

/// <summary>
/// Ambient promulgation ("irréfusabilité" — Compendium des Lois du Palais): draws and applies
/// a Palace Law on room transitions, replacing the old player-chosen "Loi" map node. Guaranteed
/// once per new "étage" (<see cref="Run.FloorIndex"/>), otherwise a <see cref="RoomRollPercent"/>%
/// chance per room traversed.
/// </summary>
public interface IAmbientPalaceLawPromulgator
{
    Task PromulgateForRoomTransitionAsync(
        Run run,
        Room nextRoom,
        CancellationToken cancellationToken = default);
}

public sealed class AmbientPalaceLawPromulgator : IAmbientPalaceLawPromulgator
{
    private const int RoomRollPercent = 20;
    private const int MaxDrawAttempts = 3;

    // Fixed rarity-tier table from the Compendium des Lois du Palais.
    private static readonly (string Rarity, int Weight)[] RarityTiers =
    [
        ("Commun", 38),
        ("Peu commun", 28),
        ("Rare", 20),
        ("Épique", 10),
        ("Légendaire", 4),
    ];

    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly DeterministicWeightedSelector _weightedSelector;

    public AmbientPalaceLawPromulgator(
        ICatalogContentGateway catalogContentGateway,
        DeterministicWeightedSelector weightedSelector)
    {
        _catalogContentGateway = catalogContentGateway;
        _weightedSelector = weightedSelector;
    }

    public async Task PromulgateForRoomTransitionAsync(
        Run run,
        Room nextRoom,
        CancellationToken cancellationToken = default)
    {
        var isGuaranteed = run.LastPromulgationFloorIndex != run.FloorIndex;

        if (!isGuaranteed && !RolledForRandomPromulgation(run, nextRoom.Id.Value))
        {
            return;
        }

        var allLaws = await _catalogContentGateway.ListActivePalaceLawDefinitionsAsync(cancellationToken);

        // Chapitre IX "lois liées aux salles" are pinned to a specific room via RoomKey and
        // sit entirely outside the ambient random draw — they're promulgated by a dedicated
        // room-entry hook, not yet built (tracked for Phase 4's catalog seed).
        var activeLawKeys = run.ActivePalaceLaws.Select(law => law.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pool = allLaws
            .Where(law => string.IsNullOrEmpty(law.RoomKey))
            .Where(law => !activeLawKeys.Contains(law.Key))
            .ToList();

        if (run.ShouldForceCompliantPromulgation)
        {
            // Soupape: ≥3 active Sévère laws forces the next draw to Clémente/DoubleTranchant.
            pool = pool
                .Where(law => law.Polarity == PalaceLawPolarity.Clemente
                    || law.Polarity == PalaceLawPolarity.DoubleTranchant)
                .ToList();
        }

        for (var attempt = 0; attempt < MaxDrawAttempts && pool.Count > 0; attempt++)
        {
            var drawn = DrawOne(run, nextRoom.Id.Value, pool, attempt);
            if (drawn is null)
            {
                break;
            }

            var law = PalaceLawMapper.CreatePalaceLaw(drawn);
            if (run.PromulgateLaw(law))
            {
                return;
            }

            // Rejected by the majeure-exclusivity or exclusion-pairs rule — drop it and re-roll.
            pool.Remove(drawn);
        }
    }

    private bool RolledForRandomPromulgation(Run run, Guid nodeId)
    {
        var context = BuildContext(run, nodeId, "RoomRoll");
        var candidates = new[]
        {
            new SelectionCandidate("draw", RoomRollPercent),
            new SelectionCandidate("skip", 100 - RoomRollPercent),
        };

        var result = _weightedSelector.Select(context, candidates);
        return result.Selected.Count > 0 && result.Selected.Single().Key == "draw";
    }

    private PalaceLawDefinitionSnapshot? DrawOne(
        Run run,
        Guid nodeId,
        IReadOnlyList<PalaceLawDefinitionSnapshot> pool,
        int attempt)
    {
        var tierContext = BuildContext(run, nodeId, $"RarityTier:{attempt}");
        var tierCandidates = RarityTiers
            .Select(tier => new SelectionCandidate(tier.Rarity, tier.Weight))
            .ToArray();

        var tierResult = _weightedSelector.Select(tierContext, tierCandidates);
        var drawnTier = tierResult.Selected.Count > 0 ? tierResult.Selected.Single().Key : null;

        var lawsInTier = drawnTier is null
            ? []
            : pool.Where(law => law.Rarity == drawnTier).ToList();

        // The rarity table above is populated ahead of the catalog seed that will actually
        // carry those tiers (Phase 4) — until then, fall back to the full pool so the ambient
        // trigger still functions against whatever laws already exist.
        var eligible = lawsInTier.Count > 0 ? lawsInTier : pool;

        var lawContext = BuildContext(run, nodeId, $"Law:{attempt}");
        var lawCandidates = eligible
            .Select(law => new SelectionCandidate(law.Key, law.BaseWeight <= 0 ? 1 : law.BaseWeight))
            .ToArray();

        var lawResult = _weightedSelector.Select(lawContext, lawCandidates);
        if (lawResult.Selected.Count == 0)
        {
            return null;
        }

        var selectedKey = lawResult.Selected.Single().Key;
        return eligible.FirstOrDefault(law => law.Key == selectedKey);
    }

    private static SelectionContext BuildContext(Run run, Guid nodeId, string purpose)
        => new(run.Id.Value, nodeId, nodeId, $"{run.Seed}:{purpose}", "PalaceLawPromulgation", purpose);
}
