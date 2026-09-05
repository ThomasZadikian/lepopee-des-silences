using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacter
{
    private const string StandardCharacterType = "Standard";
    private const string ActiveStatus = "Active";

    public const int MaxEquippedSkills = 4;

    /// <summary>
    /// The universal basic attack. Always usable in combat in addition to
    /// the equipped loadout — never counts against MaxEquippedSkills and
    /// never needs to be equipped/known. Guarantees a character can never
    /// end up with zero usable skills.
    /// </summary>
    public const string BasicSkillKey = "skill.basic.strike";

    /// <summary>
    /// Base number of active equipment slots (accessories and backpacks share the same pool —
    /// SFD "Système d'équipement et sac permanent" § 7, Annexe A point 3).
    /// </summary>
    public const int MaxEquippedWeapons = 1;
    public const int MaxEquippedAccessories = 1;
    public const int MaxEquippedRelics = 3;
    public const int MaxEquippedItems =
        MaxEquippedWeapons + MaxEquippedAccessories + MaxEquippedRelics;

    private readonly List<PlayerCharacterSkill> _skills;
    private readonly List<PlayerCharacterItem> _items;

    private PlayerCharacter(PlayerCharacterSnapshot snapshot)
    {
        Id = snapshot.Id;
        DefinitionKey = snapshot.DefinitionKey;
        DisplayName = snapshot.DisplayName;
        CharacterType = snapshot.CharacterType;
        Status = snapshot.Status;
        StatBlock = snapshot.StatBlock;
        _skills = snapshot.Skills.ToList();
        _items = snapshot.Items?.ToList() ?? [];
        StatPointsInvested = snapshot.StatPointsInvested;
        ArchetypeKey = snapshot.ArchetypeKey;
        ArchivedAtUtc = snapshot.ArchivedAtUtc;
    }

    public PlayerCharacterId Id { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public string CharacterType { get; }
    public string Status { get; }
    public string? ArchetypeKey { get; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public bool IsArchived => ArchivedAtUtc.HasValue;
    public PlayerCharacterStatBlock StatBlock { get; private set; }

    /// <summary>Historical value retained only to rehydrate pre-baseline saves. No current
    /// domain operation can increase it.</summary>
    public int StatPointsInvested { get; private set; }
    public int MaxVitality => StatBlock.MaxVitality;
    public int BaseMana => StatBlock.Mana;
    public int BaseCharge => StatBlock.Charge;
    public IReadOnlyCollection<PlayerCharacterSkill> Skills => _skills.AsReadOnly();
    public IEnumerable<string> SkillKeys => _skills.Select(s => s.SkillDefinitionKey);
    public IEnumerable<string> EquippedSkillKeys => _skills
        .Where(s => s.IsEquipped)
        .Select(s => s.SkillDefinitionKey)
        .Append(BasicSkillKey)
        .Distinct(StringComparer.OrdinalIgnoreCase);
    public int EquippedCount => _skills.Count(s =>
        s.IsEquipped && !string.Equals(s.SkillDefinitionKey, BasicSkillKey, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<PlayerCharacterItem> Items => _items.AsReadOnly();
    public IEnumerable<string> ItemKeys => _items.Select(i => i.ItemDefinitionKey);
    public IEnumerable<string> EquippedItemKeys => _items
        .Where(i => i.IsEquipped)
        .Select(i => i.ItemDefinitionKey);
    public int EquippedItemCount => _items.Count(i => i.IsEquipped);
    public IReadOnlyDictionary<EquipmentPosition, OwnedItemInstanceId> EquipmentLoadout => _items
        .Where(item => item.Position.HasValue)
        .ToDictionary(item => item.Position!.Value, item => item.Id);

    public static PlayerCharacter Create(
        string definitionKey,
        string displayName,
        int maxVitality,
        int baseMana,
        int baseCharge,
        IReadOnlyCollection<string> skillKeys)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
            throw new DomainException("Character definition key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Character display name is required.");

        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: baseMana,
            charge: baseCharge,
            magicAttack: 6,
            magicDefense: 3);

        if (skillKeys is null || skillKeys.Count == 0)
            throw new DomainException("Character must have at least one skill.");

        if (skillKeys.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Skill keys cannot be empty.");

        var now = DateTimeOffset.UtcNow;
        var skills = skillKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => PlayerCharacterSkill.Create(key, now, "default"))
            .ToArray();

        return new PlayerCharacter(new PlayerCharacterSnapshot
        {
            Id = PlayerCharacterId.New(),
            DefinitionKey = definitionKey.Trim(),
            DisplayName = displayName.Trim(),
            CharacterType = StandardCharacterType,
            Status = ActiveStatus,
            StatBlock = statBlock,
            Skills = skills
        });
    }

    public static PlayerCharacter Create(
        string definitionKey,
        string displayName,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills,
        string characterType = StandardCharacterType,
        string status = ActiveStatus)
    {
        ValidateCreation(definitionKey, displayName, statBlock, skills);

        return new PlayerCharacter(new PlayerCharacterSnapshot
        {
            Id = PlayerCharacterId.New(),
            DefinitionKey = definitionKey.Trim(),
            DisplayName = displayName.Trim(),
            CharacterType = string.IsNullOrWhiteSpace(characterType) ? StandardCharacterType : characterType.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? ActiveStatus : status.Trim(),
            StatBlock = statBlock,
            Skills = skills
        });
    }

    /// <summary>
    /// Creates the Account-owned playable character selected during onboarding.
    /// The canonical DefinitionKey may be shared by several characters; ArchetypeKey
    /// captures the immutable gameplay archetype chosen by the player.
    /// </summary>
    public static PlayerCharacter CreatePlayable(
        string definitionKey,
        string displayName,
        string archetypeKey,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills)
    {
        ValidateCreation(definitionKey, displayName, statBlock, skills);

        if (string.IsNullOrWhiteSpace(archetypeKey))
            throw new DomainException("Character archetype key is required.");

        return new PlayerCharacter(new PlayerCharacterSnapshot
        {
            Id = PlayerCharacterId.New(),
            DefinitionKey = definitionKey.Trim(),
            DisplayName = displayName.Trim(),
            CharacterType = "Player",
            Status = ActiveStatus,
            StatBlock = statBlock,
            Skills = skills,
            ArchetypeKey = archetypeKey.Trim()
        });
    }

    private static void ValidateCreation(
        string definitionKey,
        string displayName,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
            throw new DomainException("Character definition key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Character display name is required.");

        ArgumentNullException.ThrowIfNull(statBlock);

        if (skills is null || skills.Count == 0)
            throw new DomainException("Character must have at least one skill.");

        if (skills.Select(s => s.SkillDefinitionKey).Distinct(StringComparer.OrdinalIgnoreCase).Count() != skills.Count)
            throw new DomainException("Character cannot contain duplicate skills.");
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        if (ArchivedAtUtc.HasValue)
            return;

        ArchivedAtUtc = archivedAtUtc;
    }

    public void AddSkill(PlayerCharacterSkill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        if (_skills.Any(s => string.Equals(s.SkillDefinitionKey, skill.SkillDefinitionKey, StringComparison.OrdinalIgnoreCase)))
            return;

        _skills.Add(skill);
    }

    /// <summary>
    /// Semantic entry point for learning a new skill (event/combat/talent
    /// unlocks will call this once those triggers exist). Wraps the
    /// existing dedupe-by-key AddSkill.
    /// </summary>
    public void LearnSkill(PlayerCharacterSkill skill) => AddSkill(skill);

    public void EquipSkill(string skillKey)
    {
        var skill = FindSkill(skillKey);

        if (skill.IsEquipped)
            return;

        if (EquippedCount >= MaxEquippedSkills)
            throw new DomainException($"Cannot equip more than {MaxEquippedSkills} skills.");

        skill.Equip();
    }

    public void UnequipSkill(string skillKey)
    {
        FindSkill(skillKey).Unequip();
    }

    private PlayerCharacterSkill FindSkill(string skillKey)
    {
        var skill = _skills.FirstOrDefault(s => string.Equals(s.SkillDefinitionKey, skillKey, StringComparison.OrdinalIgnoreCase));

        return skill ?? throw new DomainException($"Skill '{skillKey}' is not known by this character.");
    }

    public void AddItem(PlayerCharacterItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Any(i => i.Id == item.Id))
            return;

        _items.Add(item);
    }

    public void EquipItem(OwnedItemInstanceId itemInstanceId, EquipmentPosition targetPosition)
    {
        var item = FindItem(itemInstanceId);
        if (item.Position == targetPosition)
            return;
        if (item.IsEquipped)
            throw new DomainException($"Item instance '{itemInstanceId}' is already equipped in {item.Position}.");

        // Build and validate the prospective mapping before mutating either item.
        var occupied = _items.SingleOrDefault(candidate => candidate.Position == targetPosition);
        var prospectiveIds = _items
            .Where(candidate => candidate.IsEquipped && candidate.Id != occupied?.Id)
            .Select(candidate => candidate.Id)
            .Append(item.Id)
            .ToArray();
        if (prospectiveIds.Distinct().Count() != prospectiveIds.Length)
            throw new DomainException("An item instance cannot occupy more than one equipment position.");

        occupied?.Unequip();
        item.Equip(targetPosition);
    }

    public void EquipItem(string itemKey, EquipmentSlotKind slot)
    {
        var item = FindItem(itemKey);

        if (item.IsEquipped)
            return;

        var equippedInSlot = _items.Count(i => i.IsEquipped && i.Slot == slot);
        var slotLimit = slot switch
        {
            EquipmentSlotKind.Weapon => MaxEquippedWeapons,
            EquipmentSlotKind.Accessory => MaxEquippedAccessories,
            EquipmentSlotKind.Relic => MaxEquippedRelics,
            _ => throw new DomainException($"Unsupported equipment slot '{slot}'.")
        };
        if (equippedInSlot >= slotLimit)
            throw new DomainException(
                $"Cannot equip more than {slotLimit} item(s) in slot {slot}.");

        item.Equip(NextLegacyPosition(slot));
    }

    public void EquipItem(string itemKey) => EquipItem(itemKey, EquipmentSlotKind.Relic);

    public void UnequipItem(string itemKey)
    {
        FindItem(itemKey).Unequip();
    }

    public void UnequipItem(OwnedItemInstanceId itemInstanceId)
    {
        FindItem(itemInstanceId).Unequip();
    }

    public void DetachItem(OwnedItemInstanceId itemInstanceId)
    {
        var item = FindItem(itemInstanceId);
        if (item.IsEquipped)
            throw new DomainException($"Item instance '{itemInstanceId}' must be unequipped before it can be detached.");
        _items.Remove(item);
    }

    private PlayerCharacterItem FindItem(string itemKey)
    {
        var item = _items.FirstOrDefault(i => string.Equals(i.ItemDefinitionKey, itemKey, StringComparison.OrdinalIgnoreCase));

        return item ?? throw new DomainException($"Item '{itemKey}' is not owned by this character.");
    }

    private PlayerCharacterItem FindItem(OwnedItemInstanceId itemInstanceId)
    {
        var item = _items.FirstOrDefault(candidate => candidate.Id == itemInstanceId);
        return item ?? throw new DomainException($"Item instance '{itemInstanceId}' is not owned by this character.");
    }

    private EquipmentPosition NextLegacyPosition(EquipmentSlotKind slot) => slot switch
    {
        EquipmentSlotKind.MainWeapon or EquipmentSlotKind.Weapon => EquipmentPosition.MainWeapon,
        EquipmentSlotKind.Ring or EquipmentSlotKind.Accessory => EquipmentPosition.Ring1,
        EquipmentSlotKind.Relic => new[] { EquipmentPosition.Relic, EquipmentPosition.Ring1, EquipmentPosition.Ring2 }
            .First(position => _items.All(item => item.Position != position)),
        _ => Enum.Parse<EquipmentPosition>(slot.ToString())
    };

    /// <summary>
    /// Rehydrates a player character from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay character.
    /// </summary>
    public static PlayerCharacter Rehydrate(
        PlayerCharacterId id,
        string definitionKey,
        string displayName,
        int maxVitality,
        int baseMana,
        int baseCharge,
        IReadOnlyCollection<string> skillKeys)
    {
        var statBlock = PlayerCharacterStatBlock.Create(
            maxVitality,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            focus: 0,
            mana: baseMana,
            charge: baseCharge);

        var now = DateTimeOffset.UtcNow;
        var skills = skillKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => PlayerCharacterSkill.Create(key, now, "legacy_migration", isEquipped: true))
            .ToArray();

        return Rehydrate(id, definitionKey, displayName, StandardCharacterType, ActiveStatus, statBlock, skills);
    }

    public static PlayerCharacter Rehydrate(
        PlayerCharacterId id,
        string definitionKey,
        string displayName,
        string characterType,
        string status,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills)
    {
        return Rehydrate(new PlayerCharacterSnapshot
        {
            Id = id,
            DefinitionKey = definitionKey,
            DisplayName = displayName,
            CharacterType = string.IsNullOrWhiteSpace(characterType) ? StandardCharacterType : characterType,
            Status = string.IsNullOrWhiteSpace(status) ? ActiveStatus : status,
            StatBlock = statBlock,
            Skills = skills
        });
    }

    public static PlayerCharacter Rehydrate(PlayerCharacterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new PlayerCharacter(snapshot with
        {
            CharacterType = string.IsNullOrWhiteSpace(snapshot.CharacterType)
                ? StandardCharacterType
                : snapshot.CharacterType,
            Status = string.IsNullOrWhiteSpace(snapshot.Status)
                ? ActiveStatus
                : snapshot.Status
        });
    }
}

public sealed record PlayerCharacterSnapshot
{
    public required PlayerCharacterId Id { get; init; }
    public required string DefinitionKey { get; init; }
    public required string DisplayName { get; init; }
    public required string CharacterType { get; init; }
    public required string Status { get; init; }
    public required PlayerCharacterStatBlock StatBlock { get; init; }
    public required IReadOnlyCollection<PlayerCharacterSkill> Skills { get; init; }
    public IReadOnlyCollection<PlayerCharacterItem>? Items { get; init; }
    public int StatPointsInvested { get; init; }
    public string? ArchetypeKey { get; init; }
    public DateTimeOffset? ArchivedAtUtc { get; init; }
}
