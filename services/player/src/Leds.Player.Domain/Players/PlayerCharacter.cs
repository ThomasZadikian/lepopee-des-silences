using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacter
{
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

    private PlayerCharacter(
        PlayerCharacterId id,
        string definitionKey,
        string displayName,
        string characterType,
        string status,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills,
        IReadOnlyCollection<PlayerCharacterItem>? items = null,
        int statPointsInvested = 0)
    {
        Id = id;
        DefinitionKey = definitionKey;
        DisplayName = displayName;
        CharacterType = characterType;
        Status = status;
        StatBlock = statBlock;
        _skills = skills.ToList();
        _items = items?.ToList() ?? [];
        StatPointsInvested = statPointsInvested;
    }

    public PlayerCharacterId Id { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public string CharacterType { get; }
    public string Status { get; }
    public PlayerCharacterStatBlock StatBlock { get; private set; }

    /// <summary>
    /// How many stat points have been spent on this specific character so far.
    /// Used to catch newly recruited companions up to the party's current level
    /// (see <see cref="PlayerProfile.RecruitCompanion"/>) — companion base stats are
    /// arbitrary per-NPC catalog values, so this can't be reverse-engineered from
    /// the stat block the way it could for the protagonist's fixed starting stats.
    /// </summary>
    public int StatPointsInvested { get; private set; }
    public int MaxVitality => StatBlock.MaxVitality;
    public int BaseMana => StatBlock.Mana;
    public int BaseCharge => StatBlock.Charge;
    public IReadOnlyCollection<PlayerCharacterSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<string> SkillKeys => _skills.Select(s => s.SkillDefinitionKey).ToArray();
    public IReadOnlyCollection<string> EquippedSkillKeys => _skills
        .Where(s => s.IsEquipped)
        .Select(s => s.SkillDefinitionKey)
        .Append(BasicSkillKey)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    public int EquippedCount => _skills.Count(s =>
        s.IsEquipped && !string.Equals(s.SkillDefinitionKey, BasicSkillKey, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<PlayerCharacterItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<string> ItemKeys => _items.Select(i => i.ItemDefinitionKey).ToArray();
    public IReadOnlyCollection<string> EquippedItemKeys => _items
        .Where(i => i.IsEquipped)
        .Select(i => i.ItemDefinitionKey)
        .ToArray();
    public int EquippedItemCount => _items.Count(i => i.IsEquipped);

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
            recovery: 5,
            focus: 0,
            mana: baseMana,
            charge: baseCharge,
            // Same baseline as CreateDefaultPorteur() — recruited companions used to
            // start with Magic Attack/Defense at 0 since these params were omitted here.
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

        return new PlayerCharacter(
            PlayerCharacterId.New(),
            definitionKey.Trim(),
            displayName.Trim(),
            "Standard",
            "Active",
            statBlock,
            skills);
    }

    public static PlayerCharacter Create(
        string definitionKey,
        string displayName,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills,
        string characterType = "Standard",
        string status = "Active")
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

        return new PlayerCharacter(
            PlayerCharacterId.New(),
            definitionKey.Trim(),
            displayName.Trim(),
            string.IsNullOrWhiteSpace(characterType) ? "Standard" : characterType.Trim(),
            string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim(),
            statBlock,
            skills);
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

    public void ApplyStatIncrement(PlayerStatKind kind)
    {
        StatBlock = StatBlock.WithIncrementedStat(kind);
        StatPointsInvested++;
    }

    private PlayerCharacterSkill FindSkill(string skillKey)
    {
        var skill = _skills.FirstOrDefault(s => string.Equals(s.SkillDefinitionKey, skillKey, StringComparison.OrdinalIgnoreCase));

        return skill ?? throw new DomainException($"Skill '{skillKey}' is not known by this character.");
    }

    public void AddItem(PlayerCharacterItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.Any(i => string.Equals(i.ItemDefinitionKey, item.ItemDefinitionKey, StringComparison.OrdinalIgnoreCase)))
            return;

        _items.Add(item);
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

        item.Equip(slot);
    }

    public void EquipItem(string itemKey) => EquipItem(itemKey, EquipmentSlotKind.Relic);

    public void UnequipItem(string itemKey)
    {
        FindItem(itemKey).Unequip();
    }

    private PlayerCharacterItem FindItem(string itemKey)
    {
        var item = _items.FirstOrDefault(i => string.Equals(i.ItemDefinitionKey, itemKey, StringComparison.OrdinalIgnoreCase));

        return item ?? throw new DomainException($"Item '{itemKey}' is not owned by this character.");
    }

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
            recovery: 5,
            focus: 0,
            mana: baseMana,
            charge: baseCharge);

        var now = DateTimeOffset.UtcNow;
        var skills = skillKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => PlayerCharacterSkill.Create(key, now, "legacy_migration", isEquipped: true))
            .ToArray();

        return Rehydrate(id, definitionKey, displayName, "Standard", "Active", statBlock, skills);
    }

    public static PlayerCharacter Rehydrate(
        PlayerCharacterId id,
        string definitionKey,
        string displayName,
        string characterType,
        string status,
        PlayerCharacterStatBlock statBlock,
        IReadOnlyCollection<PlayerCharacterSkill> skills,
        IReadOnlyCollection<PlayerCharacterItem>? items = null,
        int statPointsInvested = 0)
    {
        return new PlayerCharacter(
            id,
            definitionKey,
            displayName,
            string.IsNullOrWhiteSpace(characterType) ? "Standard" : characterType,
            string.IsNullOrWhiteSpace(status) ? "Active" : status,
            statBlock,
            skills,
            items,
            statPointsInvested);
    }
}
