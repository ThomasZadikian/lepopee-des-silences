using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Infrastructure.Generation.Rooms.Reachability;

public sealed class RoomReachabilitySelector : IRoomReachabilitySelector
{
    /// <summary>Weight applied to a theme pair with no explicit editorial override.</summary>
    private const decimal DefaultThemeAffinityWeight = 1.0m;

    /// <summary>
    /// Per-visit multiplicative penalty applied to a strict-chain-triggering room already
    /// visited N times this run (SFD § 6): 1 visit -&gt; x0.15, 2 visits -&gt; x0.0225, etc.
    /// Never zero — re-entering a portal room stays theoretically possible.
    /// </summary>
    private const double StrictChainRevisitDecay = 0.15;

    public CatalogRoomDefinition? SelectNextRoom(
        CatalogRoomDefinition currentRoom,
        IReadOnlyCollection<CatalogRoomDefinition> allRoomDefinitions,
        IReadOnlyCollection<CatalogWorldDefinition> worlds,
        IReadOnlyCollection<CatalogRoomThemeAffinity> themeAffinities,
        int nextRoomDepth,
        IReadOnlyCollection<string> visitedRoomKeysThisRun,
        string seed)
    {
        ArgumentNullException.ThrowIfNull(currentRoom);
        ArgumentNullException.ThrowIfNull(allRoomDefinitions);
        ArgumentNullException.ThrowIfNull(worlds);
        ArgumentNullException.ThrowIfNull(visitedRoomKeysThisRun);

        if (string.IsNullOrWhiteSpace(currentRoom.WorldKey))
        {
            return null;
        }

        var byKey = allRoomDefinitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        var eligible = ComputeEligible(currentRoom, byKey, nextRoomDepth);

        // Empty candidate list covers both the end of a strict chain (no children declared)
        // and a generation dead-end (children declared but none in the current depth range)
        // — a single fallback rule for both, per SFD § 5.4.
        if (eligible.Length == 0)
        {
            return ResolveWorldEntryRoom(currentRoom.WorldKey, worlds, byKey);
        }

        if (eligible.Length == 1)
        {
            return eligible[0];
        }

        return SelectWeighted(currentRoom, eligible, themeAffinities, visitedRoomKeysThisRun, nextRoomDepth, seed);
    }

    public IReadOnlyCollection<CatalogRoomDefinition> SelectEligibleRooms(
        CatalogRoomDefinition currentRoom,
        IReadOnlyCollection<CatalogRoomDefinition> allRoomDefinitions,
        IReadOnlyCollection<CatalogWorldDefinition> worlds,
        int nextRoomDepth)
    {
        ArgumentNullException.ThrowIfNull(currentRoom);
        ArgumentNullException.ThrowIfNull(allRoomDefinitions);
        ArgumentNullException.ThrowIfNull(worlds);

        if (string.IsNullOrWhiteSpace(currentRoom.WorldKey))
        {
            return [];
        }

        var byKey = allRoomDefinitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        var eligible = ComputeEligible(currentRoom, byKey, nextRoomDepth);

        if (eligible.Length > 0)
        {
            return eligible;
        }

        // Same "never a true dead end" fallback as SelectNextRoom, wrapped as a single exit
        // instead of a silent pick.
        var entryRoom = ResolveWorldEntryRoom(currentRoom.WorldKey, worlds, byKey);
        return entryRoom is null ? [] : [entryRoom];
    }

    private static CatalogRoomDefinition[] ComputeEligible(
        CatalogRoomDefinition currentRoom,
        IReadOnlyDictionary<string, CatalogRoomDefinition> byKey,
        int nextRoomDepth)
    {
        return currentRoom.ReachableRoomKeys
            .Select(key => byKey.GetValueOrDefault(key))
            .Where(room => room is not null)
            .Select(room => room!)
            .Where(room => nextRoomDepth >= room.MinDepth && nextRoomDepth <= room.MaxDepth)
            .ToArray();
    }

    private static CatalogRoomDefinition? ResolveWorldEntryRoom(
        string worldKey,
        IReadOnlyCollection<CatalogWorldDefinition> worlds,
        IReadOnlyDictionary<string, CatalogRoomDefinition> byKey)
    {
        var world = worlds.FirstOrDefault(w => string.Equals(w.Key, worldKey, StringComparison.OrdinalIgnoreCase));
        return world is null ? null : byKey.GetValueOrDefault(world.EntryRoomKey);
    }

    private static CatalogRoomDefinition SelectWeighted(
        CatalogRoomDefinition currentRoom,
        IReadOnlyCollection<CatalogRoomDefinition> eligible,
        IReadOnlyCollection<CatalogRoomThemeAffinity> themeAffinities,
        IReadOnlyCollection<string> visitedRoomKeysThisRun,
        int nextRoomDepth,
        string seed)
    {
        var weighted = eligible
            .OrderBy(room => room.Key, StringComparer.Ordinal) // deterministic order before the seeded roll
            .Select(room => (Room: room, Weight: ComputeWeight(currentRoom.Theme, room, themeAffinities, visitedRoomKeysThisRun)))
            .ToArray();

        var totalWeight = weighted.Sum(entry => entry.Weight);
        if (totalWeight <= 0)
        {
            return weighted[^1].Room;
        }

        var roll = DeterministicCombatRoll.UnitInterval($"{seed}|room-reachability|{nextRoomDepth}|{currentRoom.Key}");
        var target = roll * totalWeight;

        var cumulative = 0.0;
        foreach (var (room, weight) in weighted)
        {
            cumulative += weight;
            if (target < cumulative)
            {
                return room;
            }
        }

        return weighted[^1].Room;
    }

    private static double ComputeWeight(
        string currentTheme,
        CatalogRoomDefinition candidate,
        IReadOnlyCollection<CatalogRoomThemeAffinity> themeAffinities,
        IReadOnlyCollection<string> visitedRoomKeysThisRun)
    {
        var baseWeight = Math.Max(1, candidate.BaseWeight);
        var affinity = ResolveThemeAffinity(currentTheme, candidate.Theme, themeAffinities);
        var antiLoopPenalty = candidate.TriggersStrictChain
            ? Math.Pow(
                StrictChainRevisitDecay,
                visitedRoomKeysThisRun.Count(key => string.Equals(key, candidate.Key, StringComparison.OrdinalIgnoreCase)))
            : 1.0;

        return (double)(baseWeight * affinity) * antiLoopPenalty;
    }

    private static decimal ResolveThemeAffinity(
        string themeFrom, string themeTo, IReadOnlyCollection<CatalogRoomThemeAffinity> themeAffinities)
    {
        var match = themeAffinities.FirstOrDefault(a =>
            string.Equals(a.ThemeFrom, themeFrom, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.ThemeTo, themeTo, StringComparison.OrdinalIgnoreCase));
        return match?.Weight ?? DefaultThemeAffinityWeight;
    }
}
