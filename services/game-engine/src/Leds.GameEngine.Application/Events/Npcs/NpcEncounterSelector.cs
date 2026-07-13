using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.Combats.Typing;
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
            .Where(npc => IsBoundRoomCompatible(npc, context.RoomKey))
            .Where(npc => !IsAlreadyRecruitedCompanion(npc, context.RecruitedCompanionNpcKeys))
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

    // A PNJ lié (BoundRoomKeys non vide) n'est éligible que dans sa/ses Room précises —
    // contrairement aux filtres "Compatible*" ci-dessus, ce n'est pas une préférence
    // parmi d'autres mais une exigence stricte. Un PNJ générique (BoundRoomKeys vide)
    // n'est pas affecté par ce filtre.
    private static bool IsBoundRoomCompatible(CatalogNpcDefinition npc, string? roomKey)
    {
        if (npc.BoundRoomKeys.Count == 0)
            return true;
        return roomKey != null && npc.BoundRoomKeys.Contains(roomKey, StringComparer.OrdinalIgnoreCase);
    }

    // An NPC already recruited as a companion never appears again as a fresh encounter —
    // they travel with the player instead (see ResolveCurrentEventCommandHandler.RecruitedCompanionNpcKeys).
    private static bool IsAlreadyRecruitedCompanion(
        CatalogNpcDefinition npc, IReadOnlyCollection<string>? recruitedCompanionNpcKeys)
    {
        return recruitedCompanionNpcKeys is not null
            && recruitedCompanionNpcKeys.Contains(npc.Key, StringComparer.OrdinalIgnoreCase);
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
        var rollSeed = string.Join(
            context.Seed, "npc-selection",
            context.RoomDepth, context.NodeDepth,
            context.RoomType, context.PalaceRoomState);

        var roll = DeterministicCombatRoll.UnitInterval(rollSeed);
        return Math.Min(count - 1, (int)(roll * count));
    }
}
