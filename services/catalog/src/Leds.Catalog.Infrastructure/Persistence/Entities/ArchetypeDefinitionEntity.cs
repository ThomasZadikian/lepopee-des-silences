namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class ArchetypeDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string BaseStatsJson { get; set; } = "{}";
    public string ProficiencyTagsJson { get; set; } = "[]";
    public string StarterEquipmentJson { get; set; } = "[]";
    public string StarterKnownSkillsJson { get; set; } = "[]";
    public string StarterEquippedSkillsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
