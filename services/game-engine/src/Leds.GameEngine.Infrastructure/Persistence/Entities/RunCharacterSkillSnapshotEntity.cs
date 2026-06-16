namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunCharacterSkillSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public string SkillDefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SkillType { get; set; } = string.Empty;
    public string TargetingMode { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public int ChargeCost { get; set; }
    public int BasePower { get; set; }

    public RunCharacterSnapshotEntity? CharacterSnapshot { get; set; }
}
