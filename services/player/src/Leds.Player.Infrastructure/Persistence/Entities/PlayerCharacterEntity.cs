namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class PlayerCharacterEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public string DefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int MaxVitality { get; set; }
    public int BaseMana { get; set; }
    public int BaseCharge { get; set; }
    public string SkillKeysJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }

    public PlayerProfileEntity? PlayerProfile { get; set; }
}
