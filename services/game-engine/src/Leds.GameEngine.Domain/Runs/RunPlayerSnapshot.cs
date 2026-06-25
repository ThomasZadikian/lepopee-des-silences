using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunPlayerSnapshot
{
    private readonly List<RunCharacterSnapshot> _characters = [];

    private RunPlayerSnapshot(
        Guid id,
        Guid playerId,
        string displayName,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<RunCharacterSnapshot> characters)
    {
        Id = id;
        PlayerId = playerId;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
        _characters.AddRange(characters);
    }

    public Guid Id { get; }
    public Guid PlayerId { get; }
    public string DisplayName { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public IReadOnlyCollection<RunCharacterSnapshot> Characters => _characters.AsReadOnly();

    public static RunPlayerSnapshot Create(
        Guid playerId,
        string displayName,
        IReadOnlyCollection<RunCharacterSnapshot> characters,
        DateTimeOffset createdAtUtc)
    {
        if (playerId == Guid.Empty)
            throw new DomainException("Player id is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player display name is required.");

        if (characters is null || characters.Count == 0)
            throw new DomainException("Player must have at least one character snapshot.");

        return new RunPlayerSnapshot(
            Guid.NewGuid(),
            playerId,
            displayName.Trim(),
            createdAtUtc,
            characters);
    }

    public static RunPlayerSnapshot Rehydrate(
        Guid id,
        Guid playerId,
        string displayName,
        DateTimeOffset createdAtUtc,
        IReadOnlyCollection<RunCharacterSnapshot> characters)
    {
        return new RunPlayerSnapshot(id, playerId, displayName, createdAtUtc, characters);
    }

    /// <summary>DevTools/testing only: appends a companion character to the roster.</summary>
    public void DebugAddCharacter(RunCharacterSnapshot character)
    {
        ArgumentNullException.ThrowIfNull(character);
        _characters.Add(character);
    }

    /// <summary>
    /// DevTools/testing only: removes the last companion. The protagonist (index 0)
    /// is never removed. Returns false when only the protagonist remains.
    /// </summary>
    public bool DebugRemoveLastCompanion()
    {
        if (_characters.Count <= 1)
            return false;

        _characters.RemoveAt(_characters.Count - 1);
        return true;
    }
}
