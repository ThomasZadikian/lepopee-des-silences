namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class SkillDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrativeText { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty;
    public string TargetingType { get; set; } = string.Empty;
    public string TargetingMode { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public string CostType { get; set; } = "None";
    public int CostAmount { get; set; }
    public int ManaCost { get; set; }
    public int ChargeCost { get; set; }
    public int BasePower { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; } = 100;
    public int ActionCost { get; set; } = 10;
    public int CastTime { get; set; }
    public int RecoveryTime { get; set; }
    public int Cooldown { get; set; }
    public Guid? EffectSetId { get; set; }
    public int BaseWeight { get; set; } = 1;
    public string? SelectionGroup { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public EffectSetEntity? EffectSet { get; set; }
    public ICollection<SkillTagEntity> Tags { get; set; } = [];
}
