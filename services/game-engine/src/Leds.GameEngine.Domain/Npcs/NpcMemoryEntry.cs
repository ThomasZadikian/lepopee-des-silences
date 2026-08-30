using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Npcs;

/// <summary>
/// One thing a specific NPC remembers — SFD Système global de dialogues §4.2-4.3. Points at a
/// stable <see cref="Knowledge.KnowledgeEntry.Key"/> rather than embedding free text, so this
/// NPC's belief can be compared against (and can disagree with) the shared knowledge registry or
/// another NPC's memory of the same fact.
/// </summary>
public sealed record NpcMemoryEntry
{
    private NpcMemoryEntry(string knowledgeKey, MemoryScope scope, MemoryProvenance provenance)
    {
        KnowledgeKey = knowledgeKey;
        Scope = scope;
        Provenance = provenance;
    }

    public string KnowledgeKey { get; }

    public MemoryScope Scope { get; }

    public MemoryProvenance Provenance { get; }

    public static NpcMemoryEntry Create(string knowledgeKey, MemoryScope scope, MemoryProvenance provenance)
    {
        if (string.IsNullOrWhiteSpace(knowledgeKey))
        {
            throw new DomainException("A memory entry needs a knowledge key.");
        }

        return new NpcMemoryEntry(knowledgeKey.Trim(), scope, provenance);
    }
}
