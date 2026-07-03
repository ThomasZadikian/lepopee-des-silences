using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerRoster
{
    private readonly List<PlayerCharacter> _characters;

    private PlayerRoster(IReadOnlyCollection<PlayerCharacter> characters)
    {
        _characters = characters.ToList();
    }

    public IReadOnlyCollection<PlayerCharacter> Characters => _characters.AsReadOnly();

    public static PlayerRoster Create()
    {
        return new PlayerRoster([]);
    }

    public void AddCharacter(PlayerCharacter character)
    {
        ArgumentNullException.ThrowIfNull(character);

        if (_characters.Any(c => c.Id == character.Id))
            throw new DomainException($"Character with id '{character.Id}' already exists in the roster.");

        if (_characters.Any(c => c.DefinitionKey == character.DefinitionKey))
            throw new DomainException($"Character with definition key '{character.DefinitionKey}' already exists in the roster.");

        _characters.Add(character);
    }

    public IReadOnlyCollection<PlayerCharacter> GetAvailableCharacters()
    {
        return _characters.AsReadOnly();
    }

    public PlayerCharacter? FindById(PlayerCharacterId id)
    {
        return _characters.FirstOrDefault(c => c.Id == id);
    }

    public PlayerCharacter GetRequired(PlayerCharacterId id)
    {
        return FindById(id) ?? throw new DomainException($"Character '{id}' not found in roster.");
    }

    /// <summary>
    /// Rehydrates a player roster from a trusted persistence snapshot.
    /// This method must not be used to create a new gameplay roster.
    /// </summary>
    public static PlayerRoster Rehydrate(IReadOnlyCollection<PlayerCharacter> characters)
    {
        return new PlayerRoster(characters);
    }
}
