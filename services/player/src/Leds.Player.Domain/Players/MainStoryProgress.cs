using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

/// <summary>Permanent account progress. It is deliberately independent from any temporary Run.</summary>
public sealed class MainStoryProgress
{
    private readonly HashSet<string> _unlockedRoomKeys;
    private readonly HashSet<string> _visibleRoomKeys;

    private MainStoryProgress(
        string? sequenceKey,
        string? sequenceVersion,
        string? stepKey,
        string? checkpointKey,
        bool isCompleted,
        int highestDifficultyLevelUnlocked,
        IEnumerable<string>? unlockedRoomKeys,
        IEnumerable<string>? visibleRoomKeys)
    {
        SequenceKey = sequenceKey;
        SequenceVersion = sequenceVersion;
        StepKey = stepKey;
        CheckpointKey = checkpointKey;
        IsCompleted = isCompleted;
        HighestDifficultyLevelUnlocked = highestDifficultyLevelUnlocked;
        _unlockedRoomKeys = new HashSet<string>(unlockedRoomKeys ?? [], StringComparer.OrdinalIgnoreCase);
        _visibleRoomKeys = new HashSet<string>(visibleRoomKeys ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public string? SequenceKey { get; private set; }
    public string? SequenceVersion { get; private set; }
    public string? StepKey { get; private set; }
    public string? CheckpointKey { get; private set; }
    public bool IsCompleted { get; private set; }
    public int HighestDifficultyLevelUnlocked { get; private set; }
    public IReadOnlySet<string> UnlockedRoomKeys => _unlockedRoomKeys;
    public IReadOnlySet<string> VisibleRoomKeys => _visibleRoomKeys;

    public static MainStoryProgress CreateDefault() =>
        new(null, null, null, null, false, 0, null, null);

    public void Advance(string sequenceKey, string sequenceVersion, string stepKey, string? checkpointKey)
    {
        if (IsCompleted)
            throw new DomainException("Completed Main Story progress cannot advance.");
        if (string.IsNullOrWhiteSpace(sequenceKey)
            || string.IsNullOrWhiteSpace(sequenceVersion)
            || string.IsNullOrWhiteSpace(stepKey))
            throw new DomainException("Story sequence, version and step are required.");

        SequenceKey = sequenceKey.Trim();
        SequenceVersion = sequenceVersion.Trim();
        StepKey = stepKey.Trim();
        CheckpointKey = string.IsNullOrWhiteSpace(checkpointKey) ? null : checkpointKey.Trim();
    }

    public bool UnlockRoom(string roomKey)
    {
        if (string.IsNullOrWhiteSpace(roomKey))
            throw new DomainException("Room unlock key is required.");
        return _unlockedRoomKeys.Add(roomKey.Trim());
    }

    public bool RevealRoom(string roomKey)
    {
        if (string.IsNullOrWhiteSpace(roomKey))
            throw new DomainException("Visible room key is required.");
        return _visibleRoomKeys.Add(roomKey.Trim());
    }

    public void Complete()
    {
        IsCompleted = true;
        HighestDifficultyLevelUnlocked = Math.Max(1, HighestDifficultyLevelUnlocked);
    }

    public bool UnlockNextDifficulty(int level)
    {
        if (!IsCompleted)
            throw new DomainException("Difficulty levels unlock only after the Main Story.");
        if (level <= HighestDifficultyLevelUnlocked)
            return false;
        if (level != HighestDifficultyLevelUnlocked + 1)
            throw new DomainException("Difficulty levels must unlock sequentially through mastery.");

        HighestDifficultyLevelUnlocked = level;
        return true;
    }

    public static MainStoryProgress Rehydrate(
        string? sequenceKey,
        string? sequenceVersion,
        string? stepKey,
        string? checkpointKey,
        bool isCompleted,
        int highestDifficultyLevelUnlocked,
        IEnumerable<string>? unlockedRoomKeys,
        IEnumerable<string>? visibleRoomKeys) =>
        new(sequenceKey, sequenceVersion, stepKey, checkpointKey, isCompleted,
            highestDifficultyLevelUnlocked, unlockedRoomKeys, visibleRoomKeys);
}
