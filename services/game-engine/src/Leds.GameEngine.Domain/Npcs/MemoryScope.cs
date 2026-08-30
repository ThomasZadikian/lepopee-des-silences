namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// How long a <see cref="NpcMemoryEntry"/> or <see cref="Knowledge.KnowledgeVersion"/> lasts
/// before it should be forgotten — SFD Système global de dialogues §4.2. Pruning at each
/// boundary (conversation end, room exit, run end) is an Application-layer responsibility (see
/// <see cref="NpcRelationship.ForgetScope"/>); the domain only tags the scope.
/// </summary>
public enum MemoryScope
{
    /// <summary>Forgotten once the current conversation ends.</summary>
    Conversation = 0,

    /// <summary>Forgotten once the party leaves the room it was learned in.</summary>
    Room = 1,

    /// <summary>Forgotten at the end of the run.</summary>
    Run = 2,

    /// <summary>Persists across runs — meta-progression memory.</summary>
    Account = 3,
}
