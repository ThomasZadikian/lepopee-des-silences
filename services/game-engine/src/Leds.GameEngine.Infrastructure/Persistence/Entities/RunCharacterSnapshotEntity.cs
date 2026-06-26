namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunCharacterSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid PlayerSnapshotId { get; set; }
    public Guid CharacterId { get; set; }
    public string DefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SnapshotOrder { get; set; }
    public RunPlayerSnapshotEntity? PlayerSnapshot { get; set; }
    public RunCharacterStatSnapshotEntity? StatBlock { get; set; }
    public List<RunCharacterSkillSnapshotEntity> Skills { get; set; } = [];
}
