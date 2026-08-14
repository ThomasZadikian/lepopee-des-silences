using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Npcs;

namespace Leds.GameEngine.Domain.Knowledge;

/// <summary>
/// One fact/person/place/event referenced by a stable <see cref="Key"/> — SFD Système global de
/// dialogues §5. Several contradictory <see cref="KnowledgeVersion"/>s can be tracked at once;
/// confirming one settles the entry.
/// </summary>
public sealed class KnowledgeEntry
{
    private readonly List<KnowledgeVersion> _versions;

    private KnowledgeEntry(string key, KnowledgeCategory category, List<KnowledgeVersion> versions)
    {
        Key = key;
        Category = category;
        _versions = versions;
    }

    /// <summary>Stable identity — never regenerated, so an NPC's memory or another entry can
    /// reference it durably.</summary>
    public string Key { get; }

    public KnowledgeCategory Category { get; }

    public IReadOnlyList<KnowledgeVersion> Versions => _versions;

    /// <summary>The settled version, if any — null while versions still contradict each other.</summary>
    public KnowledgeVersion? ConfirmedVersion => _versions.FirstOrDefault(v => v.Provenance == MemoryProvenance.Confirmed);

    public bool IsConfirmed => ConfirmedVersion is not null;

    public static KnowledgeEntry Create(string key, KnowledgeCategory category)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("A knowledge entry needs a key.");
        }

        return new KnowledgeEntry(key.Trim(), category, []);
    }

    public static KnowledgeEntry Rehydrate(string key, KnowledgeCategory category, IEnumerable<KnowledgeVersion> versions) =>
        new(key, category, versions.ToList());

    /// <summary>
    /// Adds a candidate version. Once an entry is confirmed the fact is settled — a further
    /// contradicting version has nothing left to contradict, so this throws rather than silently
    /// reopening a resolved fact.
    /// </summary>
    public void AddVersion(KnowledgeVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (IsConfirmed)
        {
            throw new DomainException($"Knowledge entry '{Key}' is already confirmed and cannot take another version.");
        }

        if (version.Provenance == MemoryProvenance.Confirmed)
        {
            _versions.Clear();
        }

        _versions.Add(version);
    }
}
