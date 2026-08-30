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

        // DefinitionKey identifies the underlying canonical character, not a unique account slot.
        // Several playable Characters may therefore share character.player.self while differing by
        // immutable archetype and display name. Companion deduplication remains enforced by the
        // recruitment use case before it reaches the roster.
        _characters.Add(character);
    }

    public IReadOnlyCollection<PlayerCharacter> GetAvailableCharacters()
    {
        return _characters.Where(c => !c.IsArchived).ToList().AsReadOnly();
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
