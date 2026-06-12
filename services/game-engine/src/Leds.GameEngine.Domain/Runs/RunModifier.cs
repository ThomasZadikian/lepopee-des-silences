using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

/// <summary>
/// A transient run-scoped modifier that affects combat or reward calculations.
/// Examples: guard shard (+8 starting guard), curse (+10% enemy difficulty for next combat).
/// </summary>
public sealed class RunModifier
{
    private RunModifier(
        RunModifierId id,
        RunModifierType type,
        double value,
        RunModifierDuration duration,
        string sourceType,
        string sourceKey,
        DateTime createdAtUtc,
        DateTime? consumedAtUtc)
    {
        Id = id;
        Type = type;
        Value = value;
        Duration = duration;
        SourceType = sourceType;
        SourceKey = sourceKey;
        CreatedAtUtc = createdAtUtc;
        ConsumedAtUtc = consumedAtUtc;
    }

    public RunModifierId Id { get; }

    /// <summary>The mechanical effect category of this modifier.</summary>
    public RunModifierType Type { get; }

    /// <summary>
    /// The magnitude of the modifier. Interpretation depends on <see cref="Type"/>.
    /// For <see cref="RunModifierType.StartingGuardBonus"/>, this is the raw guard points added.
    /// For multiplier types, this is the delta (0.10 = +10%).
    /// </summary>
    public double Value { get; }

    public RunModifierDuration Duration { get; }

    /// <summary>
    /// Category of the source that created this modifier (e.g., "RunItem", "Curse", "PalaceLaw").
    /// </summary>
    public string SourceType { get; }

    /// <summary>The definition key of the source (e.g., "item.guard-shard-v1").</summary>
    public string SourceKey { get; }

    public DateTime CreatedAtUtc { get; }

    /// <summary>Timestamp at which this modifier was consumed. Null while still active.</summary>
    public DateTime? ConsumedAtUtc { get; private set; }

    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public static RunModifier Create(
        RunModifierType type,
        double value,
        RunModifierDuration duration,
        string sourceType,
        string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new DomainException("RunModifier source type is required.");

        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new DomainException("RunModifier source key is required.");

        return new RunModifier(
            RunModifierId.New(),
            type,
            value,
            duration,
            sourceType.Trim(),
            sourceKey.Trim(),
            DateTime.UtcNow,
            consumedAtUtc: null);
    }

    public static RunModifier Rehydrate(
        RunModifierId id,
        RunModifierType type,
        double value,
        RunModifierDuration duration,
        string sourceType,
        string sourceKey,
        DateTime createdAtUtc,
        DateTime? consumedAtUtc)
    {
        return new RunModifier(id, type, value, duration, sourceType, sourceKey, createdAtUtc, consumedAtUtc);
    }

    /// <summary>
    /// Marks this modifier as consumed at the given timestamp.
    /// Idempotent — calling it on an already-consumed modifier is a no-op.
    /// </summary>
    public void Consume(DateTime consumedAt)
    {
        if (IsConsumed)
        {
            return;
        }

        ConsumedAtUtc = consumedAt;
    }
}
