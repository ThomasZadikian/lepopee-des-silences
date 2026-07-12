using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Types;

/// <summary>
/// Resolves the next room type as a catalog theme key (e.g. "Memory", "Rupture",
/// "Fear"). The vocabulary AND the rotation come entirely from the catalog
/// <c>RoomTypeDefinition</c> set — adding a room type in the catalog adds it to the
/// rotation, nothing is hardcoded (SAL-4: "catalogue source de vérité").
/// <para>
/// The transition keeps the Markov property (it is biased toward the current type)
/// but is computed directly via a seeded deterministic roll rather than the rigid
/// N×N <c>MarkovTransitionMatrix</c>, so a dynamically-built vocabulary can never
/// produce an invalid (non-stochastic) matrix that would throw at runtime.
/// </para>
/// </summary>
public interface ICatalogRoomTypeResolver
{
    Task<string> ResolveNextRoomTypeKeyAsync(
        string seed,
        int nextRoomDepth,
        string currentRoomTypeKey,
        CancellationToken cancellationToken = default,
        int bossInterval = CatalogMarkovRoomTypeResolver.BossInterval);
}

public sealed class CatalogMarkovRoomTypeResolver : ICatalogRoomTypeResolver
{
    public const string ThresholdTheme = "Threshold";
    public const string FinalTheme = "Final";

    // Him'Lit boss room recurs every BossInterval rooms (endless run, no max depth).
    public const int BossInterval = 10;

    // Integer weights: the current type is favoured (self-bias) without ever excluding
    // the others. Integer math keeps the deterministic pick exact (no decimal drift).
    private const int BaseWeight = 10;
    private const int SelfBias = 6;

    private readonly ICatalogContentGateway _catalogContentGateway;

    public CatalogMarkovRoomTypeResolver(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<string> ResolveNextRoomTypeKeyAsync(
        string seed,
        int nextRoomDepth,
        string currentRoomTypeKey,
        CancellationToken cancellationToken = default,
        int bossInterval = BossInterval)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed is required.", nameof(seed));
        }

        if (bossInterval <= 0)
        {
            bossInterval = BossInterval;
        }

        // Depth 0 is always the Threshold (run entrance).
        if (nextRoomDepth <= 0)
        {
            return ThresholdTheme;
        }

        // Him'Lit boss room recurs every bossInterval rooms (10, 20, 30, … — or tighter
        // when the player owns Mina's "Protection de Him'Lit", see DeterministicRunGenerator).
        if (nextRoomDepth % bossInterval == 0)
        {
            return FinalTheme;
        }

        var types = await _catalogContentGateway.ListRoomTypeDefinitionsAsync(cancellationToken);

        // Transitionable targets: every catalog theme except the entrance (Threshold)
        // and the boss (Final), filtered by the room's depth window.
        var targets = types
            .Where(t => !string.Equals(t.Theme, ThresholdTheme, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(t.Theme, FinalTheme, StringComparison.OrdinalIgnoreCase))
            .Where(t => nextRoomDepth >= t.MinDepth && nextRoomDepth <= t.MaxDepth)
            .Select(t => t.Theme)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        // No catalog vocabulary yet → fall back to Threshold; the generator maps it to
        // a neutral procedural scaffold, so generation never stalls.
        if (targets.Length == 0)
        {
            return ThresholdTheme;
        }

        if (targets.Length == 1)
        {
            return targets[0];
        }

        var weights = new int[targets.Length];
        var totalWeight = 0;
        for (var i = 0; i < targets.Length; i++)
        {
            weights[i] = BaseWeight + (string.Equals(targets[i], currentRoomTypeKey, StringComparison.OrdinalIgnoreCase)
                ? SelfBias
                : 0);
            totalWeight += weights[i];
        }

        var roll = DeterministicCombatRoll.UnitInterval($"{seed}|room-type|{nextRoomDepth}|{currentRoomTypeKey}");
        var target = roll * totalWeight;

        var cumulative = 0.0;
        for (var i = 0; i < targets.Length; i++)
        {
            cumulative += weights[i];
            if (target < cumulative)
            {
                return targets[i];
            }
        }

        return targets[^1];
    }
}
