namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class SkillTemplateEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public int ChargeCost { get; set; }
    public int BasePower { get; set; }
    public int HealPower { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
