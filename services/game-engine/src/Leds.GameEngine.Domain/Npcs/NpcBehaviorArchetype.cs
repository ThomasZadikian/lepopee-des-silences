namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// How a <see cref="RoomNpc"/> moves through its room, independent of the story it carries —
/// Contrat canonique §39 ("selon leur type, les ennemis peuvent être fixes, gardiens,
/// patrouilleurs, chasseurs, passifs"), generalized here to every exploration NPC, hostile or
/// not, since the same taxonomy already covers the Majordome (a guardian/greeter) as well as an
/// ambient habitant.
/// </summary>
public enum NpcBehaviorArchetype
{
    /// <summary>Never moves from its spawn cell.</summary>
    Fixed = 0,

    /// <summary>Stays within a short leash of its spawn cell — a post, not a route.</summary>
    Guardian = 1,

    /// <summary>Cycles a fixed, authored list of waypoints, in order, forever.</summary>
    Patrol = 2,

    /// <summary>Idles like <see cref="Guardian"/> until <see cref="NpcAwarenessState.Aware"/> or
    /// above, then closes the distance to the party one step at a time.</summary>
    Hunter = 3,

    /// <summary>Wanders on its own business near its spawn cell; never reacts to the party.</summary>
    Passive = 4,
}
