namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class CatalogSeedVersionEntity
{
    public Guid Id { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Checksum { get; set; }
    public DateTime AppliedAtUtc { get; set; }
}
