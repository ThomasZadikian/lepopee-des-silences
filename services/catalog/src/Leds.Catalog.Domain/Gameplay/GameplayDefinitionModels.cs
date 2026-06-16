using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Gameplay;

public sealed record CatalogVersion(string SeedKey, string Version)
{
    public CatalogVersion() : this(string.Empty, string.Empty) { }

    public static CatalogVersion Create(string seedKey, string version)
    {
        GameplayGuard.Require(seedKey, "Catalog seed key is required.");
        GameplayGuard.Require(version, "Catalog version is required.");
        return new CatalogVersion(seedKey.Trim(), version.Trim());
    }
}

public sealed record CatalogTag(string TagKey, string DisplayName, string? Category)
{
    public static CatalogTag Create(string tagKey, string displayName, string? category = null)
    {
        GameplayGuard.Require(tagKey, "Catalog tag key is required.");
        GameplayGuard.Require(displayName, "Catalog tag display name is required.");
        return new CatalogTag(tagKey.Trim(), displayName.Trim(), GameplayGuard.TrimToNull(category));
    }
}

public sealed record EnemyStatBlock(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus)
{
    public static EnemyStatBlock Create(
        int maxVitality,
        int attackPower,
        int defense,
        int startingGuard,
        int speed,
        int initiative = 0,
        int recovery = 0,
        int focus = 0)
    {
        if (maxVitality <= 0) throw new DomainException("Enemy max vitality must be greater than 0.");
        if (attackPower < 0) throw new DomainException("Enemy attack power cannot be negative.");
        if (defense < 0) throw new DomainException("Enemy defense cannot be negative.");
        if (startingGuard < 0) throw new DomainException("Enemy starting guard cannot be negative.");
        if (speed <= 0) throw new DomainException("Enemy speed must be greater than 0.");
        if (initiative < 0) throw new DomainException("Enemy initiative cannot be negative.");
        if (recovery < 0) throw new DomainException("Enemy recovery cannot be negative.");
        if (focus < 0) throw new DomainException("Enemy focus cannot be negative.");

        return new EnemyStatBlock(maxVitality, attackPower, defense, startingGuard, speed, initiative, recovery, focus);
    }
}

public sealed record EnemySkillLink(string SkillDefinitionKey)
{
    public static EnemySkillLink Create(string skillDefinitionKey)
    {
        GameplayGuard.Require(skillDefinitionKey, "Enemy skill definition key is required.");
        return new EnemySkillLink(skillDefinitionKey.Trim());
    }
}

public sealed record EnemyTag(string TagKey)
{
    public static EnemyTag Create(string tagKey)
    {
        GameplayGuard.Require(tagKey, "Enemy tag key is required.");
        return new EnemyTag(tagKey.Trim());
    }
}

public sealed record EffectSet(
    string Key,
    string DisplayName,
    string? Description,
    string Version,
    IReadOnlyCollection<EffectDefinition> Effects)
{
    public static EffectSet Create(
        string key,
        string displayName,
        string? description,
        string version,
        IReadOnlyCollection<EffectDefinition> effects)
    {
        GameplayGuard.Require(key, "Effect set key is required.");
        GameplayGuard.Require(displayName, "Effect set display name is required.");
        GameplayGuard.Require(version, "Effect set version is required.");

        return new EffectSet(
            key.Trim(),
            displayName.Trim(),
            GameplayGuard.TrimToNull(description),
            version.Trim(),
            effects.OrderBy(e => e.Order).ToArray());
    }
}

public sealed record EffectDefinition(
    string EffectType,
    string TargetScope,
    decimal? Value,
    string ValueMode,
    string Duration,
    string StackPolicy,
    string? Condition,
    int Order,
    string? BehaviorTag,
    string? GenerationTag,
    string? SelectionGroup)
{
    public static EffectDefinition Create(
        string effectType,
        string targetScope,
        decimal? value,
        string valueMode,
        string duration,
        string stackPolicy,
        int order,
        string? behaviorTag = null,
        string? generationTag = null,
        string? selectionGroup = null,
        string? condition = null)
    {
        GameplayGuard.Require(effectType, "Effect type is required.");
        GameplayGuard.Require(targetScope, "Effect target scope is required.");
        GameplayGuard.Require(valueMode, "Effect value mode is required.");
        GameplayGuard.Require(duration, "Effect duration is required.");
        GameplayGuard.Require(stackPolicy, "Effect stack policy is required.");

        return new EffectDefinition(
            effectType.Trim(),
            targetScope.Trim(),
            value,
            valueMode.Trim(),
            duration.Trim(),
            stackPolicy.Trim(),
            GameplayGuard.TrimToNull(condition),
            order,
            GameplayGuard.TrimToNull(behaviorTag),
            GameplayGuard.TrimToNull(generationTag),
            GameplayGuard.TrimToNull(selectionGroup));
    }
}

public sealed record CurseDefinition(
    string Key,
    string DisplayName,
    string Description,
    int Severity,
    string Duration,
    string Version)
{
    public static CurseDefinition Create(string key, string displayName, string description, int severity, string duration, string version)
    {
        GameplayGuard.Require(key, "Curse key is required.");
        GameplayGuard.Require(displayName, "Curse display name is required.");
        GameplayGuard.Require(description, "Curse description is required.");
        GameplayGuard.Require(duration, "Curse duration is required.");
        GameplayGuard.Require(version, "Curse version is required.");
        if (severity <= 0) throw new DomainException("Curse severity must be greater than 0.");

        return new CurseDefinition(key.Trim(), displayName.Trim(), description.Trim(), severity, duration.Trim(), version.Trim());
    }
}

public sealed record RewardTemplate(string Key, string DisplayName, IReadOnlyCollection<RewardTemplateOption> Options)
{
    public static RewardTemplate Create(string key, string displayName, IReadOnlyCollection<RewardTemplateOption> options)
    {
        GameplayGuard.Require(key, "Reward template key is required.");
        GameplayGuard.Require(displayName, "Reward template display name is required.");
        if (options.Count == 0) throw new DomainException("Reward template must define at least one option.");

        return new RewardTemplate(key.Trim(), displayName.Trim(), options.ToArray());
    }
}

public sealed record RewardTemplateOption(string RewardType, string Label, string Description, string? PayloadKey)
{
    public static RewardTemplateOption Create(string rewardType, string label, string description, string? payloadKey = null)
    {
        GameplayGuard.Require(rewardType, "Reward option type is required.");
        GameplayGuard.Require(label, "Reward option label is required.");
        GameplayGuard.Require(description, "Reward option description is required.");
        return new RewardTemplateOption(rewardType.Trim(), label.Trim(), description.Trim(), GameplayGuard.TrimToNull(payloadKey));
    }
}

public sealed record RoomDefinition(
    string Key,
    string DisplayName,
    string Description,
    string RoomFamily,
    string RoomRarity,
    string Theme,
    bool IsCulturalEcho)
{
    public static RoomDefinition Create(
        string key,
        string displayName,
        string description,
        string roomFamily,
        string roomRarity,
        string theme,
        bool isCulturalEcho = false)
    {
        GameplayGuard.Require(key, "Room key is required.");
        GameplayGuard.Require(displayName, "Room display name is required.");
        GameplayGuard.Require(description, "Room description is required.");
        GameplayGuard.Require(roomFamily, "Room family is required.");
        GameplayGuard.Require(roomRarity, "Room rarity is required.");
        GameplayGuard.Require(theme, "Room theme is required.");

        return new RoomDefinition(key.Trim(), displayName.Trim(), description.Trim(), roomFamily.Trim(), roomRarity.Trim(), theme.Trim(), isCulturalEcho);
    }
}

public sealed record RoomTypeDefinition(string Key, string DisplayName, string Theme, string DefaultRarity);
public sealed record RoomBossDefinition(string Key, string DisplayName, string RoomType, string EnemyDefinitionKey);
public sealed record RoomSpecialMechanic(string Key, string DisplayName, string MechanicType);
public sealed record RoomTag(string TagKey);
public sealed record RoomEnemyPool(string Key, IReadOnlyCollection<RoomEnemyPoolEntry> Entries);
public sealed record RoomEnemyPoolEntry(string EnemyDefinitionKey, int Weight, int MinCount, int MaxCount);
public sealed record RoomRewardPool(string Key, IReadOnlyCollection<RoomRewardPoolEntry> Entries);
public sealed record RoomRewardPoolEntry(string? RewardTemplateKey, string? ItemDefinitionKey, int Weight);
public sealed record RoomLawPool(string Key, IReadOnlyCollection<RoomLawPoolEntry> Entries);
public sealed record RoomLawPoolEntry(string LawDefinitionKey, int Weight);
public sealed record RoomCursePool(string Key, IReadOnlyCollection<RoomCursePoolEntry> Entries);
public sealed record RoomCursePoolEntry(string CurseDefinitionKey, int Weight);

internal static class GameplayGuard
{
    public static void Require(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }
    }

    public static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
