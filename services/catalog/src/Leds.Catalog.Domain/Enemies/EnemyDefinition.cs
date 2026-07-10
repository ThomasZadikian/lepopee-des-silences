using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Enemies;

public sealed class EnemyDefinition : CatalogContentBase, IEnemyDefinition
{
    private readonly List<string> _compatibleRoomTypes;
    private readonly List<string> _tags;
    private readonly List<string> _skillKeys;

    private EnemyDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        string archetype,
        int baseDifficulty,
        int minRiskLevel,
        int maxRiskLevel,
        int attackPower,
        int defense,
        int speed,
        int focus,
        IReadOnlyCollection<string> compatibleRoomTypes,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> skillKeys)
        : base(id, key, name, description, version, status)
    {
        Archetype = archetype;
        BaseDifficulty = baseDifficulty;
        MinRiskLevel = minRiskLevel;
        MaxRiskLevel = maxRiskLevel;
        AttackPower = attackPower;
        Defense = defense;
        Speed = speed;
        Focus = focus;
        _compatibleRoomTypes = compatibleRoomTypes.ToList();
        _tags = tags.ToList();
        _skillKeys = skillKeys.ToList();
    }

    public string Archetype { get; }

    public int BaseDifficulty { get; }

    public int MinRiskLevel { get; }

    public int MaxRiskLevel { get; }

    public int AttackPower { get; }

    public int Defense { get; }

    public int Speed { get; }

    public int Focus { get; }

    public IReadOnlyCollection<string> CompatibleRoomTypes => _compatibleRoomTypes.AsReadOnly();

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<string> SkillKeys => _skillKeys.AsReadOnly();

    public static EnemyDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        string archetype,
        int baseDifficulty,
        int minRiskLevel,
        int maxRiskLevel,
        IReadOnlyCollection<string>? compatibleRoomTypes,
        IReadOnlyCollection<string>? tags,
        IReadOnlyCollection<string>? skillKeys,
        CatalogContentStatus status = CatalogContentStatus.Draft,
        int attackPower = 0,
        int defense = 0,
        int speed = 10,
        int focus = 0)
    {
        var desc = CatalogContentDescription.From(description);

        if (desc.IsEmpty)
        {
            throw new DomainException("Enemy definition description is required.");
        }

        if (string.IsNullOrWhiteSpace(archetype))
        {
            throw new DomainException("Enemy definition archetype is required.");
        }

        if (baseDifficulty <= 0)
        {
            throw new DomainException("Enemy definition base difficulty must be greater than 0.");
        }

        if (minRiskLevel < 1)
        {
            throw new DomainException("Enemy definition min risk level must be at least 1.");
        }

        if (maxRiskLevel < minRiskLevel)
        {
            throw new DomainException("Enemy definition max risk level must be greater than or equal to min risk level.");
        }

        if (attackPower < 0)
        {
            throw new DomainException("Enemy definition attack power cannot be negative.");
        }

        if (defense < 0)
        {
            throw new DomainException("Enemy definition defense cannot be negative.");
        }

        if (speed < 1)
        {
            throw new DomainException("Enemy definition speed must be at least 1.");
        }

        if (focus < 0)
        {
            throw new DomainException("Enemy definition focus cannot be negative.");
        }

        var distinctRoomTypes = compatibleRoomTypes is null || compatibleRoomTypes.Count == 0
            ? throw new DomainException("Enemy definition must have at least one compatible room type.")
            : compatibleRoomTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var distinctTags = tags is null || tags.Count == 0
            ? Array.Empty<string>()
            : tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var distinctSkillKeys = skillKeys is null || skillKeys.Count == 0
            ? Array.Empty<string>()
            : skillKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new EnemyDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            desc,
            CatalogContentVersion.From(version),
            status,
            archetype.Trim(),
            baseDifficulty,
            minRiskLevel,
            maxRiskLevel,
            attackPower,
            defense,
            speed,
            focus,
            distinctRoomTypes,
            distinctTags,
            distinctSkillKeys);
    }
}
