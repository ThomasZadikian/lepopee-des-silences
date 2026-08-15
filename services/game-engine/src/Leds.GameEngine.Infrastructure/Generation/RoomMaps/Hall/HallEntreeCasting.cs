using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;

/// <summary>
/// The Hall d'entrée's authored population — SFD Hall d'entrée §IV, ported cell-for-cell from
/// the reference implementation's <c>cast()</c> roster. Catalog NPC keys beyond
/// <c>npc.majordome</c> (the only one already Catalog-seeded) are placeholders pending real
/// Catalog authoring — <see cref="Npcs.RoomNpc"/> never validates its
/// <see cref="Npcs.RoomNpc.CatalogNpcKey"/> against the Catalog at creation time, so this doesn't
/// block the structural work; it just means dialogue for these isn't wired yet.
/// <para>
/// Keys follow the frontend bestiaire's own convention (<c>salle-casting.js</c>: "un id de
/// casting peut porter une variante : 'emotion#5', 'habitant#3'") rather than the ad-hoc dotted
/// names this class first shipped with — <c>useCombatantSprites.ts</c>'s <c>figureIdFor</c>
/// strips a leading <c>npc.</c> and looks the remainder straight up in the bestiaire roster, so
/// <c>npc.habitant#0</c> resolves to the <c>habitant</c> figure's first variant with no new
/// resolution logic, where <c>npc.habitant.hall</c> would not have resolved at all.
/// </para>
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
        // Variants 0/1 of the bestiaire's `habitant` figure (redingote/robe).
        new Entry("npc.habitant#0", 4, 6, NpcBehaviorArchetype.Passive, 0),
        new Entry("npc.habitant#1", 5, 7, NpcBehaviorArchetype.Passive, 0),

        // Variant 3 ("visiteur") of the same `habitant` figure.
        new Entry("npc.habitant#3", 20, 7, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        // Variant 2 ("livrée") — a servant's cut, the closest the shared `habitant` figure has
        // to a dedicated "Porteur de Plateau" (the reference's habitants family has no figure of
        // its own for this role).
        new Entry("npc.habitant#2", 22, 6, NpcBehaviorArchetype.Passive, 3),
        new Entry("npc.veilleur-tapis", 15, 15, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        // Variant 5 ("silence") of the bestiaire's `emotion` figure — SFD Hall §III/IV: this
        // Écho guards the east threshold in the silence register.
        new Entry("npc.emotion#5", 23, 10, NpcBehaviorArchetype.Guardian, RoomNpc.DefaultAwarenessRadius),
        new Entry("npc.chat#0", 17, 12, NpcBehaviorArchetype.Passive, 0),
        new Entry("npc.chien", 9, 14, NpcBehaviorArchetype.Guardian, 2),
    ];
}
