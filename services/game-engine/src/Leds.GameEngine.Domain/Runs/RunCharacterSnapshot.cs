using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunCharacterSnapshot
{
    private readonly List<RunCharacterSkillSnapshot> _skills = [];

    private RunCharacterSnapshot(
        Guid id,
        Guid characterId,
        string definitionKey,
        string displayName,
        RunCharacterStatSnapshot statBlock,
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills,
        IReadOnlyCollection<string>? equippedItemKeys = null)
    {
        Id = id;
        CharacterId = characterId;
        DefinitionKey = definitionKey;
        DisplayName = displayName;
        StatBlock = statBlock;
        _skills.AddRange(skills);
        EquippedItemKeys = NormalizeEquippedItemKeys(equippedItemKeys);
    }

    public Guid Id { get; }
    public Guid CharacterId { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public RunCharacterStatSnapshot StatBlock { get; }
    public IReadOnlyCollection<RunCharacterSkillSnapshot> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<string> EquippedItemKeys { get; private set; }

    public static RunCharacterSnapshot Create(
        Guid characterId,
        string definitionKey,
        string displayName,
        RunCharacterStatSnapshot statBlock,
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills,
        IReadOnlyCollection<string>? equippedItemKeys = null)
    {
        if (characterId == Guid.Empty)
            throw new DomainException("Character id is required.");

        if (string.IsNullOrWhiteSpace(definitionKey))
            throw new DomainException("Character definition key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Character display name is required.");

        ArgumentNullException.ThrowIfNull(statBlock);
        skills ??= [];

        return new RunCharacterSnapshot(
            Guid.NewGuid(),
            characterId,
            definitionKey.Trim(),
            displayName.Trim(),
            statBlock,
            skills,
            equippedItemKeys);
    }

    /// <summary>
    /// Grimoire mid-run resync: replaces this companion's effective combat skills so the
    /// next combat casts with the freshly-validated selection instead of the loadout
    /// frozen at StartRun (see <see cref="Run.ReplaceCharacterSkills"/>).
    /// </summary>
    public void ReplaceSkills(IReadOnlyCollection<RunCharacterSkillSnapshot> skills)
    {
        if (skills is null || skills.Count == 0)
            throw new DomainException("Character must have at least one skill.");

        _skills.Clear();
        _skills.AddRange(skills);
    }

    /// <summary>
    /// Equipment mid-run resync (equip/unequip a permanent item from the run): replaces
    /// this character's snapshotted item-key list so equipment-keyed triggers (e.g.
    /// <c>equipment.*</c> combat triggers, conditional room/weather item effects) read
    /// the freshly-equipped loadout instead of the one frozen at <see cref="Run.StartNew"/>.
    /// </summary>
    public void ReplaceEquippedItemKeys(IReadOnlyCollection<string>? equippedItemKeys)
    {
        EquippedItemKeys = NormalizeEquippedItemKeys(equippedItemKeys);
    }

    private static IReadOnlyCollection<string> NormalizeEquippedItemKeys(
        IReadOnlyCollection<string>? equippedItemKeys)
    {
        return (equippedItemKeys ?? [])
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static RunCharacterSnapshot Rehydrate(
        Guid id,
        Guid characterId,
        string definitionKey,
        string displayName,
        RunCharacterStatSnapshot statBlock,
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills,
        IReadOnlyCollection<string>? equippedItemKeys = null)
    {
        return new RunCharacterSnapshot(
            id, characterId, definitionKey, displayName, statBlock, skills, equippedItemKeys);
    }
}
