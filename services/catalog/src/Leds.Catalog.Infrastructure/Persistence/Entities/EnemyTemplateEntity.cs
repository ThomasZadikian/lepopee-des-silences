namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class EnemyTemplateEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Speed { get; set; }
    public int PhysicalResistance { get; set; }
    public int MagicalResistance { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
