namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class RewardCursePoolEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EntriesJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}