namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class PalaceLawDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Scope { get; set; } = "Run";
    public string Duration { get; set; } = "UntilRunEnds";
    public string? Trigger { get; set; }
    public int Severity { get; set; } = 1;
    public string Visibility { get; set; } = string.Empty;
    public int Priority { get; set; }
    public Guid? EffectSetId { get; set; }
    public int BaseWeight { get; set; } = 1;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string? SelectionGroup { get; set; }
    public string ImpactDomainsJson { get; set; } = "[]";

    /// <summary>Display-only rarity tier (Commun/PeuCommun/Rare/Epique/Legendaire) — the actual
    /// draw weight is <see cref="BaseWeight"/>, resolved against the fixed rarity-tier table in
    /// the promulgation engine, not this string.</summary>
    public string Rarity { get; set; } = "Commun";

    /// <summary>Clemente/Severe/DoubleTranchant/Neutre — drives the Soupape rule (3+ active
    /// Severe laws forces the next promulgation to Clemente or DoubleTranchant).</summary>
    public string Polarity { get; set; } = "Neutre";

    /// <summary>Chapitre VIII "lois majeures" — at most one may be active at a time.</summary>
    public bool IsMajeure { get; set; }

    /// <summary>Chapitre IX "lois liées aux salles" — non-null pins this law to a single
    /// catalog room key; it is then exempt from the cumul cap (<see cref="IsCumulExempt"/>)
    /// and only ever promulgated when entering that room.</summary>
    public string? RoomKey { get; set; }

    public bool IsCumulExempt { get; set; }

    /// <summary>JSON array of other law keys this law cannot coexist with (e.g. Troisième
    /// Tasse / Souvenir Doux) — re-rolled on conflict at promulgation time.</summary>
    public string ExclusionKeysJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<LawTagEntity> Tags { get; set; } = [];
}
