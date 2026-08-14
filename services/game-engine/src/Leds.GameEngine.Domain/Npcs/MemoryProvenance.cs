namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// How a <see cref="NpcMemoryEntry"/> or <see cref="Knowledge.KnowledgeVersion"/> was learned —
/// SFD Système global de dialogues §4.3/§5. Shared vocabulary between the two: a knowledge
/// version becoming <see cref="Confirmed"/> is what ends the "contradictory versions coexist"
/// window described in §5.
/// </summary>
public enum MemoryProvenance
{
    /// <summary>The NPC witnessed it directly.</summary>
    Observed = 0,

    /// <summary>The player said it — may be a lie (see the future lying extension).</summary>
    PlayerStated = 1,

    /// <summary>Another NPC said it.</summary>
    NpcStated = 2,

    /// <summary>Secondhand, unverified.</summary>
    Rumor = 3,

    /// <summary>Settled — no longer contradicted by another surviving version.</summary>
    Confirmed = 4,
}
