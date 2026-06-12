namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class EnemyDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public int BaseDifficulty { get; set; }
    public int MinRiskLevel { get; set; }
    public int MaxRiskLevel { get; set; }
    public string CompatibleRoomTypesJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";
    public string SkillKeysJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
