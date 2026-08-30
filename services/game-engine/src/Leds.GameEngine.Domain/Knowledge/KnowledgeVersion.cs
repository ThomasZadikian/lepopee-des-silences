using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Domain.Knowledge;

/// <summary>
/// One candidate value for a <see cref="KnowledgeEntry"/> — several can coexist, contradicting
/// each other, until one becomes <see cref="MemoryProvenance.Confirmed"/> (SFD Système global de
/// dialogues §5).
/// </summary>
public sealed record KnowledgeVersion
{
    private KnowledgeVersion(string value, MemoryProvenance provenance)
    {
        Value = value;
        Provenance = provenance;
    }

    public string Value { get; }

    public MemoryProvenance Provenance { get; }

    public static KnowledgeVersion Create(string value, MemoryProvenance provenance)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("A knowledge version needs a value.");
        }

        return new KnowledgeVersion(value.Trim(), provenance);
    }
}
