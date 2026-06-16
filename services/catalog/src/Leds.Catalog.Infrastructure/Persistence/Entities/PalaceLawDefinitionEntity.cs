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
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<LawTagEntity> Tags { get; set; } = [];
}
