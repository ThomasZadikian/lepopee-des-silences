namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public sealed class StorySequenceDefinitionEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string EntryStepKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<StoryStepDefinitionEntity> Steps { get; set; } = [];
}

public sealed class StoryStepDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid SequenceDefinitionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? RoomDefinitionKey { get; set; }
    public string ConditionsJson { get; set; } = "[]";
    public string EffectsJson { get; set; } = "[]";
    public bool IsTerminal { get; set; }
    public StorySequenceDefinitionEntity SequenceDefinition { get; set; } = null!;
}
