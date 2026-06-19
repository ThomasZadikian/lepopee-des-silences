namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class EventTemplateEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DefaultOutcomeKind { get; set; } = string.Empty;
    public int MinRiskLevel { get; set; }
    public int MaxRiskLevel { get; set; }
    public bool RequiresPlayerChoice { get; set; }
    public string NarrativeTagsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
