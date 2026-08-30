using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public enum RunProgressionMode
{
    Standard = 1,
    Story = 2
}

public enum StoryDifficulty
{
    Canonical = 1
}

public readonly record struct DifficultyLevel
{
    private DifficultyLevel(int value) => Value = value;

    public int Value { get; }

    public static DifficultyLevel Create(int value)
    {
        if (value < 1)
            throw new DomainException("Difficulty level N must be greater than or equal to 1.");
        return new DifficultyLevel(value);
    }
}

public sealed record StoryRunOverlay(
    string? SequenceKey,
    string? SequenceVersion,
    string? StepKey,
    string? CheckpointKey);
