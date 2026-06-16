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
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills)
    {
        Id = id;
        CharacterId = characterId;
        DefinitionKey = definitionKey;
        DisplayName = displayName;
        StatBlock = statBlock;
        _skills.AddRange(skills);
    }

    public Guid Id { get; }
    public Guid CharacterId { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public RunCharacterStatSnapshot StatBlock { get; }
    public IReadOnlyCollection<RunCharacterSkillSnapshot> Skills => _skills.AsReadOnly();

    public static RunCharacterSnapshot Create(
        Guid characterId,
        string definitionKey,
        string displayName,
        RunCharacterStatSnapshot statBlock,
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills)
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
            skills);
    }

    public static RunCharacterSnapshot Rehydrate(
        Guid id,
        Guid characterId,
        string definitionKey,
        string displayName,
        RunCharacterStatSnapshot statBlock,
        IReadOnlyCollection<RunCharacterSkillSnapshot> skills)
    {
        return new RunCharacterSnapshot(id, characterId, definitionKey, displayName, statBlock, skills);
    }
}
