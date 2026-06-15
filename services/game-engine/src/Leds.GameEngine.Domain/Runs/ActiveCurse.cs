namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// Represents an active curse modifier applied to the run.
/// MVP: simple modifier that affects the next combat's difficulty.
/// </summary>
public sealed class ActiveCurse
{
    private ActiveCurse(
        string key,
        string displayName,
        string description,
        double difficultyDelta,
        DateTime appliedAtUtc)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
        DifficultyDelta = difficultyDelta;
        AppliedAtUtc = appliedAtUtc;
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public double DifficultyDelta { get; }
    public DateTime AppliedAtUtc { get; }

    public static ActiveCurse Create(
        string key,
        string displayName,
        string description,
        double difficultyDelta,
        DateTime appliedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new Leds.GameEngine.Domain.Common.DomainException("Curse key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new Leds.GameEngine.Domain.Common.DomainException("Curse display name is required.");

        if (difficultyDelta is < -0.5 or > 0.5)
            throw new Leds.GameEngine.Domain.Common.DomainException("Curse difficulty delta must be between -0.5 and 0.5.");

        return new ActiveCurse(key, displayName, description, difficultyDelta, appliedAtUtc);
    }

    /// <summary>
    /// Rehydrates an active curse from a trusted persistence snapshot.
    /// </summary>
    public static ActiveCurse Rehydrate(
        string key,
        string displayName,
        string description,
        double difficultyDelta,
        DateTime appliedAtUtc)
    {
        return new ActiveCurse(key, displayName, description, difficultyDelta, appliedAtUtc);
    }
}