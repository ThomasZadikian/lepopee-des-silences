using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Domain.PalaceLaws;

public sealed class PalaceLaw
{
    private readonly List<PalaceLawDomain> _domains;
    private readonly List<PalaceLawEffect> _effects;
    private readonly List<string> _exclusionKeys;

    private PalaceLaw(
        PalaceLawId id,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains,
        IReadOnlyCollection<PalaceLawEffect> effects,
        string rarity,
        string polarity,
        bool isMajeure,
        string? roomKey,
        bool isCumulExempt,
        IReadOnlyCollection<string> exclusionKeys)
    {
        Id = id;
        Key = key;
        Name = name;
        Version = version;
        _domains = domains.ToList();
        _effects = effects.ToList();
        Rarity = rarity;
        Polarity = polarity;
        IsMajeure = isMajeure;
        RoomKey = roomKey;
        IsCumulExempt = isCumulExempt;
        _exclusionKeys = exclusionKeys.ToList();
    }

    public PalaceLawId Id { get; }
    public string Key { get; }
    public string Name { get; }
    public string Version { get; }
    public IReadOnlyCollection<PalaceLawDomain> Domains => _domains.AsReadOnly();

    /// <summary>
    /// Mechanical effects applied to the run when this law is accepted.
    /// Each effect maps to a <see cref="RunModifier"/> added to the run.
    /// Empty for narrative-only laws.
    /// </summary>
    public IReadOnlyCollection<PalaceLawEffect> Effects => _effects.AsReadOnly();

    /// <summary>Display-only rarity tier — the actual promulgation draw weight lives in the
    /// catalog's BaseWeight, resolved against the fixed rarity-tier table.</summary>
    public string Rarity { get; }

    /// <summary>Clemente/Severe/DoubleTranchant/Neutre — drives the Soupape rule.</summary>
    public string Polarity { get; }

    /// <summary>Chapitre VIII "lois majeures" — at most one may be active at a time.</summary>
    public bool IsMajeure { get; }

    /// <summary>Chapitre IX "lois liées aux salles" — non-null pins this law to a single room key.</summary>
    public string? RoomKey { get; }

    /// <summary>True for room-linked laws — exempt from the cumul cap.</summary>
    public bool IsCumulExempt { get; }

    /// <summary>Other law keys this law cannot coexist with.</summary>
    public IReadOnlyCollection<string> ExclusionKeys => _exclusionKeys.AsReadOnly();

    public static PalaceLaw Create(
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains,
        IReadOnlyCollection<PalaceLawEffect>? effects = null,
        string rarity = "Commun",
        string polarity = "Neutre",
        bool isMajeure = false,
        string? roomKey = null,
        bool isCumulExempt = false,
        IReadOnlyCollection<string>? exclusionKeys = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Palace law key is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Palace law name is required.");

        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("Palace law version is required.");

        if (domains is null || domains.Count == 0)
            throw new DomainException("A palace law must target at least one domain.");

        return new PalaceLaw(
            PalaceLawId.New(),
            key.Trim(),
            name.Trim(),
            version.Trim(),
            domains.Distinct().ToArray(),
            effects ?? [],
            rarity,
            polarity,
            isMajeure,
            roomKey,
            isCumulExempt,
            exclusionKeys ?? []);
    }
}