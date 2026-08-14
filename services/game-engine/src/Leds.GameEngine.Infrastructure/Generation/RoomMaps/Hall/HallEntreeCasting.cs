using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;

/// <summary>
/// The Hall d'entrée's authored population — SFD Hall d'entrée §IV, ported cell-for-cell from
/// the reference implementation's <c>cast()</c> roster. Catalog NPC keys beyond
/// <c>npc.majordome</c> (the only one already Catalog-seeded) are placeholders pending real
/// Catalog authoring — <see cref="Npcs.RoomNpc"/> never validates its
/// <see cref="Npcs.RoomNpc.CatalogNpcKey"/> against the Catalog at creation time, so this doesn't
/// block the structural work; it just means dialogue/portraits for these aren't wired yet.
/// </summary>
public static class HallEntreeCasting
{
    public sealed record Entry(
        string CatalogNpcKey,
        int X,
        int Y,
        NpcBehaviorArchetype Behavior,
        int AwarenessRadius);

    public static IReadOnlyList<Entry> Roster { get; } =
    [
        // Toujours présent (SFD §IV/§VIII: "Majordome obligatoire") — se promène puis
        // s'approche progressivement pour accueillir : le plus proche des archétypes existants
        // est Hunter (immobile tant qu'Unaware, puis referme la distance une fois Aware).
        new Entry("npc.majordome", 12, 13, NpcBehaviorArchetype.Hunter, RoomNpc.DefaultAwarenessRadius),

        // Population d'ambiance, non nommée — "certaines entités peuvent ne pas percevoir le
        // joueur" (SFD §IV) : AwarenessRadius=0 les rend structurellement incapables de
        // remarquer le groupe d'elles-mêmes (RoomNpc.RefreshAwareness's own contract).
        new Entry("npc.habitant.hall", 4, 6, NpcBehaviorArchetype.Passive, 0),
        new Entry("npc.habitant.hall", 5, 7, NpcBehaviorArchetype.Passive, 0),

        new Entry("npc.visiteur.hall", 20, 7, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        new Entry("npc.porteur-plateau", 22, 6, NpcBehaviorArchetype.Passive, 3),
        new Entry("npc.veilleur.tapis", 15, 15, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        new Entry("npc.echo-emotion", 23, 10, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        new Entry("npc.chat.palais", 17, 12, NpcBehaviorArchetype.Passive, 0),
        new Entry("npc.chien.palais", 9, 14, NpcBehaviorArchetype.Guardian, 2),
    ];
}
