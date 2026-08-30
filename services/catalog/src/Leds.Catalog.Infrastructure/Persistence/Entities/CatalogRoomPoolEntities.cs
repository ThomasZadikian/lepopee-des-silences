namespace Leds.Catalog.Infrastructure.Persistence.Entities;

public abstract class RoomPoolEntityBase
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string? SelectionGroup { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public abstract class RoomPoolEntryEntityBase
{
    public Guid Id { get; set; }
    public int Weight { get; set; } = 1;
    public int? MinDepth { get; set; }
    public int? MaxDepth { get; set; }
    public string? RequiredTag { get; set; }
    public string? ExcludedTag { get; set; }
}

public sealed class RoomEnemyPoolEntity : RoomPoolEntityBase
{
    public ICollection<RoomEnemyPoolEntryEntity> Entries { get; set; } = [];
}

public sealed class RoomEnemyPoolEntryEntity : RoomPoolEntryEntityBase
{
    public Guid PoolId { get; set; }
    public string EnemyDefinitionKey { get; set; } = string.Empty;
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;

    public RoomEnemyPoolEntity Pool { get; set; } = null!;
}

public sealed class RoomRewardPoolEntity : RoomPoolEntityBase
{
    public ICollection<RoomRewardPoolEntryEntity> Entries { get; set; } = [];
}

public sealed class RoomRewardPoolEntryEntity : RoomPoolEntryEntityBase
{
    public Guid PoolId { get; set; }
    public string? RewardTemplateKey { get; set; }
    public string? ItemDefinitionKey { get; set; }

    public RoomRewardPoolEntity Pool { get; set; } = null!;
}

public sealed class RoomLawPoolEntity : RoomPoolEntityBase
{
    public ICollection<RoomLawPoolEntryEntity> Entries { get; set; } = [];
}

public sealed class RoomLawPoolEntryEntity : RoomPoolEntryEntityBase
{
    public Guid PoolId { get; set; }
    public string LawDefinitionKey { get; set; } = string.Empty;

    public RoomLawPoolEntity Pool { get; set; } = null!;
}

public sealed class RoomCursePoolEntity : RoomPoolEntityBase
{
    public ICollection<RoomCursePoolEntryEntity> Entries { get; set; } = [];
}

public sealed class RoomCursePoolEntryEntity : RoomPoolEntryEntityBase
{
    public Guid PoolId { get; set; }
    public string CurseDefinitionKey { get; set; } = string.Empty;

    public RoomCursePoolEntity Pool { get; set; } = null!;
}
