namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class PlayerCharacterEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public string DefinitionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CharacterType { get; set; } = "Standard";
    public string Status { get; set; } = "Active";
    public int MaxVitality { get; set; }
    public int BaseMana { get; set; }
    public int BaseCharge { get; set; }
    public int StatPointsInvested { get; set; }
    public string SkillKeysJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public PlayerProfileEntity? PlayerProfile { get; set; }
    public PlayerCharacterStatBlockEntity? StatBlock { get; set; }
    public List<PlayerCharacterSkillEntity> Skills { get; set; } = [];
    public List<PlayerCharacterItemEntity> Items { get; set; } = [];
}
