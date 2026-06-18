using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Application.Events.Npcs;

public sealed class NpcEncounterSelector : INpcEncounterSelector
{
    public CatalogNpcDefinition? SelectEligibleNpc(
        NpcEligibilityContext context,
        IReadOnlyCollection<CatalogNpcDefinition> allNpcs)
    {
        if (allNpcs.Count == 0)
            return null;

        var eligible = allNpcs
            .Where(npc => IsDepthCompatible(npc, context.NodeDepth))
            .Where(npc => IsRoomTypeCompatible(npc, context.RoomType))
            .ToArray();

        var constrained = eligible
            .Where(npc => HasConstraints(npc))
            .ToArray();

        var specificMatches = constrained
            .Where(npc => MatchesPalaceState(npc, context.PalaceRoomState)
                       && MatchesClimate(npc, context.RoomClimate))
            .ToArray();

        if (specificMatches.Length > 0)
            return PickOne(specificMatches, context);

        var fallback = eligible
            .Where(npc => !HasConstraints(npc))
            .ToArray();

        if (fallback.Length > 0)
            return PickOne(fallback, context);

        return null;
    }

    private static bool HasConstraints(CatalogNpcDefinition npc)
    {
        return npc.CompatiblePalaceRoomStates.Count > 0
            || npc.CompatibleRoomClimates.Count > 0
            || npc.CompatibleRoomTypes.Count > 0;
    }

    private static bool IsDepthCompatible(CatalogNpcDefinition npc, int depth)
    {
        return depth >= npc.MinDepth && depth <= npc.MaxDepth;
    }

    private static bool IsRoomTypeCompatible(CatalogNpcDefinition npc, RoomType roomType)
    {
        if (npc.CompatibleRoomTypes.Count == 0)
            return true;
        var typeName = roomType.ToString();
        return npc.CompatibleRoomTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool MatchesPalaceState(CatalogNpcDefinition npc, PalaceRoomState state)
    {
        return npc.CompatiblePalaceRoomStates.Count == 0
            || npc.CompatiblePalaceRoomStates.Contains(state);
    }

    private static bool MatchesClimate(CatalogNpcDefinition npc, string? climate)
    {
        return npc.CompatibleRoomClimates.Count == 0
            || (climate != null && npc.CompatibleRoomClimates.Contains(climate, StringComparer.OrdinalIgnoreCase));
    }

    private static CatalogNpcDefinition PickOne(
        IReadOnlyCollection<CatalogNpcDefinition> candidates,
        NpcEligibilityContext context)
    {
        var ordered = candidates
            .OrderBy(npc => npc.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var index = ComputeDeterministicIndex(context, ordered.Length);
        return ordered[index];
    }

    private static int ComputeDeterministicIndex(NpcEligibilityContext context, int count)
    {
        unchecked
        {
            var hash = context.Seed.GetHashCode();
            hash = hash * 397 ^ context.RunId.GetHashCode();
            hash = hash * 397 ^ context.RoomId.GetHashCode();
            hash = hash * 397 ^ context.NodeId.GetHashCode();
            hash = hash * 397 ^ "NpcSelection".GetHashCode();
            return Math.Abs(hash % count);
        }
    }
}
