using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacter
{
    private readonly List<string> _skillKeys;

    private PlayerCharacter(
        PlayerCharacterId id,
        string definitionKey,
        string displayName,
        int maxVitality,
        int baseMana,
        int baseCharge,
        IReadOnlyCollection<string> skillKeys)
    {
        Id = id;
        DefinitionKey = definitionKey;
        DisplayName = displayName;
        MaxVitality = maxVitality;
        BaseMana = baseMana;
        BaseCharge = baseCharge;
        _skillKeys = skillKeys.ToList();
    }

    public PlayerCharacterId Id { get; }
    public string DefinitionKey { get; }
    public string DisplayName { get; }
    public int MaxVitality { get; }
    public int BaseMana { get; }
    public int BaseCharge { get; }
    public IReadOnlyCollection<string> SkillKeys => _skillKeys.AsReadOnly();

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

        if (maxVitality <= 0)
            throw new DomainException("Max vitality must be greater than zero.");

        if (baseMana < 0)
            throw new DomainException("Base mana cannot be negative.");

        if (baseCharge < 0)
            throw new DomainException("Base charge cannot be negative.");

        if (skillKeys is null || skillKeys.Count == 0)
            throw new DomainException("Character must have at least one skill.");

        if (skillKeys.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Skill keys cannot be empty.");

        return new PlayerCharacter(
            PlayerCharacterId.New(),
            definitionKey.Trim(),
            displayName.Trim(),
            maxVitality,
            baseMana,
            baseCharge,
            skillKeys);
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
        return new PlayerCharacter(id, definitionKey, displayName, maxVitality, baseMana, baseCharge, skillKeys);
    }
}
