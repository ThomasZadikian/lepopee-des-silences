using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Domain.Enemies;

public sealed class EnemyDefinition : CatalogContentBase, IEnemyDefinition
{
    private readonly List<string> _compatibleRoomTypes;
    private readonly List<string> _tags;
    private readonly List<string> _skillKeys;
    private readonly List<string> _boundRoomKeys;

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
        IReadOnlyCollection<string> skillKeys,
        int initiative,
        int mana,
        int magicAttack,
        int magicDefense,
        int menace,
        string rarity,
        string? registre,
        IReadOnlyCollection<string> boundRoomKeys,
        int movement)
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
        Initiative = initiative;
        Mana = mana;
        MagicAttack = magicAttack;
        MagicDefense = magicDefense;
        Menace = menace;
        Rarity = rarity;
        Registre = registre;
        _boundRoomKeys = boundRoomKeys.ToList();
        Movement = movement;
    }

    public string Archetype { get; }

    public int BaseDifficulty { get; }

    public int MinRiskLevel { get; }

    public int MaxRiskLevel { get; }

    public int AttackPower { get; }

    public int Defense { get; }

    public int Speed { get; }
    public int Movement { get; }

    public int Focus { get; }

    /// <summary>Bestiaire authoring — previously provisioned in the stat block but
    /// never mapped through to the domain object.</summary>
    public int Initiative { get; }

    /// <summary>Bestiaire authoring — previously provisioned in the stat block but
    /// never mapped through to the domain object, and never consumed by combat
    /// (enemies always started at 0 mana regardless).</summary>
    public int Mana { get; }

    /// <summary>Authored base stat mirroring AttackPower, driving Magic-category
    /// skill damage (see CombatSkillEffectResolver.StatModifierDamageMultiplier
    /// on the game-engine side).</summary>
    public int MagicAttack { get; }

    /// <summary>Authored base stat mirroring Defense, driving Magic-category
    /// skill damage (see CombatSkillEffectResolver.StatModifierDamageMultiplier
    /// on the game-engine side).</summary>
    public int MagicDefense { get; }

    /// <summary>Encounter threat-budget weight (1-10 by Bestiaire convention),
    /// used to size encounter compositions — distinct from BaseDifficulty
    /// (vitality/power scaling).</summary>
    public int Menace { get; }

    /// <summary>Bestiaire rarity tier (Common/Uncommon/Rare/MiniBoss by convention)
    /// — distinct from the pre-existing Rank, which the encounter-budget algorithm
    /// already reads for a different purpose.</summary>
    public string Rarity { get; }

    /// <summary>Emotional register, mirrors NPCs' EmotionalRegister as a free
    /// string (same convention as Archetype/Family/Role on this entity).</summary>
    public string? Registre { get; }

    public IReadOnlyCollection<string> CompatibleRoomTypes => _compatibleRoomTypes.AsReadOnly();

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<string> SkillKeys => _skillKeys.AsReadOnly();

    /// <summary>Precise room-key binding, mirrors NPCs' BoundRoomKeys. Additive to
    /// the coarser CompatibleRoomTypes category match — when non-empty, encounter
    /// selection should prefer/require these specific rooms (see game-engine's
    /// EncounterCompositionPolicy).</summary>
    public IReadOnlyCollection<string> BoundRoomKeys => _boundRoomKeys.AsReadOnly();

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
        int focus = 0,
        int initiative = 0,
        int mana = 0,
        int magicAttack = 0,
        int magicDefense = 0,
        int menace = 0,
        string rarity = "Common",
        string? registre = null,
        IReadOnlyCollection<string>? boundRoomKeys = null,
        int movement = 4)
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

        if (movement < 1)
        {
            throw new DomainException("Enemy definition movement must be at least 1.");
        }

        if (focus < 0)
        {
            throw new DomainException("Enemy definition focus cannot be negative.");
        }

        if (initiative < 0)
        {
            throw new DomainException("Enemy definition initiative cannot be negative.");
        }

        if (mana < 0)
        {
            throw new DomainException("Enemy definition mana cannot be negative.");
        }

        if (magicAttack < 0)
        {
            throw new DomainException("Enemy definition magic attack cannot be negative.");
        }

        if (magicDefense < 0)
        {
            throw new DomainException("Enemy definition magic defense cannot be negative.");
        }

        if (menace < 0)
        {
            throw new DomainException("Enemy definition menace cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(rarity))
        {
            throw new DomainException("Enemy definition rarity is required.");
        }

        var hasKnownRegister = EmotionalRegisterCatalog.TryParse(registre, out var parsedRegister);

        if (status == CatalogContentStatus.Active && !hasKnownRegister)
        {
            throw new DomainException("Active enemy definition emotional register must reference a known Catalog register.");
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

        var distinctBoundRoomKeys = boundRoomKeys is null || boundRoomKeys.Count == 0
            ? Array.Empty<string>()
            : boundRoomKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

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
            distinctSkillKeys,
            initiative,
            mana,
            magicAttack,
            magicDefense,
            menace,
            rarity.Trim(),
            hasKnownRegister ? parsedRegister.ToString() : null,
            distinctBoundRoomKeys,
            movement);
    }
}
