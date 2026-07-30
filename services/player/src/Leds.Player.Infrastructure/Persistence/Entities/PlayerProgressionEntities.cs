namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class PlayerCharacterStatBlockEntity
{
    public Guid Id { get; set; }
    public Guid PlayerCharacterId { get; set; }
    public int MaxVitality { get; set; } = 100;
    public int AttackPower { get; set; } = 12;
    public int Defense { get; set; } = 6;
    public int StartingGuard { get; set; }
    public int Speed { get; set; } = 10;
    public int Initiative { get; set; } = 10;
    public int Focus { get; set; }
    public int Mana { get; set; }
    public int Charge { get; set; }
    public int MagicAttack { get; set; }
    public int MagicDefense { get; set; }

    public PlayerCharacterEntity PlayerCharacter { get; set; } = null!;
}

public sealed class PlayerCharacterSkillEntity
{
    public Guid Id { get; set; }
    public Guid PlayerCharacterId { get; set; }
    public string SkillDefinitionKey { get; set; } = string.Empty;
    public DateTimeOffset UnlockedAtUtc { get; set; }
    public string? Source { get; set; }
    public bool IsEquipped { get; set; }
    public string EquipmentSlot { get; set; } = "Relic";

    public PlayerCharacterEntity PlayerCharacter { get; set; } = null!;
}

public sealed class PlayerPermanentUnlockEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public string UnlockKey { get; set; } = string.Empty;
    public string UnlockType { get; set; } = string.Empty;
    public Guid? SourceRunId { get; set; }
    public DateTimeOffset UnlockedAtUtc { get; set; }

    public PlayerProfileEntity PlayerProfile { get; set; } = null!;
}

public sealed class PlayerCharacterItemEntity
{
    public Guid Id { get; set; }
    public Guid PlayerCharacterId { get; set; }
    public string ItemDefinitionKey { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public string? Source { get; set; }
    public bool IsEquipped { get; set; }
    public string EquipmentSlot { get; set; } = "Relic";

    public PlayerCharacterEntity PlayerCharacter { get; set; } = null!;
}

public sealed class PlayerPermanentItemEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public string ItemDefinitionKey { get; set; } = string.Empty;
    public Guid? SourceRunId { get; set; }
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public string? ContainedLiquidDefinitionKey { get; set; }

    public PlayerProfileEntity PlayerProfile { get; set; } = null!;
}

public sealed class PlayerRunStatisticEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public Guid RunId { get; set; }
    public string Seed { get; set; } = string.Empty;
    public int FinalDepth { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string GeneratorVersion { get; set; } = string.Empty;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalDamageTaken { get; set; }
    public int TotalGuardAbsorbed { get; set; }
    public int TotalHealingDone { get; set; }
    public int CombatsWon { get; set; }
    public int CombatsLost { get; set; }
    public int RewardsSelected { get; set; }
    public int TotalItemsUsed { get; set; }

    public PlayerProfileEntity PlayerProfile { get; set; } = null!;
}

public sealed class PlayerNpcReputationScoreEntity
{
    public Guid Id { get; set; }
    public Guid PlayerProfileId { get; set; }
    public string NpcKey { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TimesMet { get; set; }
    public string? CurrentDialogueNodeKey { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public PlayerProfileEntity PlayerProfile { get; set; } = null!;
}
