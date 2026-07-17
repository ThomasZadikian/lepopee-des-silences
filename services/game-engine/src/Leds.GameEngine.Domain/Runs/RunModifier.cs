using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Effects;

namespace Leds.GameEngine.Domain.Runs;

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
        DateTime? consumedAtUtc,
        string valueMode,
        string stackPolicy,
        Guid? expiresAtRoomId,
        Guid? expiresAtCombatId,
        int? expiresAtFloorIndex)
    {
        Id = id;
        Type = type;
        Value = value;
        Duration = duration;
        SourceType = sourceType;
        SourceKey = sourceKey;
        CreatedAtUtc = createdAtUtc;
        ConsumedAtUtc = consumedAtUtc;
        ValueMode = valueMode;
        StackPolicy = stackPolicy;
        ExpiresAtRoomId = expiresAtRoomId;
        ExpiresAtCombatId = expiresAtCombatId;
        ExpiresAtFloorIndex = expiresAtFloorIndex;
    }

    public RunModifierId Id { get; }

    public RunModifierType Type { get; }

    public double Value { get; }

    public RunModifierDuration Duration { get; }

    public string SourceType { get; }

    public string SourceKey { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? ConsumedAtUtc { get; private set; }

    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public string ValueMode { get; }

    public string StackPolicy { get; }

    public Guid? ExpiresAtRoomId { get; }

    public Guid? ExpiresAtCombatId { get; }

    /// <summary>
    /// <see cref="Run.FloorIndex"/> at which this modifier expires, for
    /// <see cref="RunModifierDuration.UntilFloorEnds"/>. Null otherwise.
    /// </summary>
    public int? ExpiresAtFloorIndex { get; }

    public static RunModifier Create(
        RunModifierType type,
        double value,
        RunModifierDuration duration,
        string sourceType,
        string sourceKey,
        string valueMode = "Flat",
        string stackPolicy = "Additive",
        Guid? expiresAtRoomId = null,
        Guid? expiresAtCombatId = null,
        int? expiresAtFloorIndex = null)
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
            consumedAtUtc: null,
            valueMode,
            stackPolicy,
            expiresAtRoomId,
            expiresAtCombatId,
            expiresAtFloorIndex);
    }

    public static RunModifier Rehydrate(
        RunModifierId id,
        RunModifierType type,
        double value,
        RunModifierDuration duration,
        string sourceType,
        string sourceKey,
        DateTime createdAtUtc,
        DateTime? consumedAtUtc,
        string valueMode = "Flat",
        string stackPolicy = "Additive",
        Guid? expiresAtRoomId = null,
        Guid? expiresAtCombatId = null,
        int? expiresAtFloorIndex = null)
    {
        return new RunModifier(
            id, type, value, duration, sourceType, sourceKey,
            createdAtUtc, consumedAtUtc,
            valueMode, stackPolicy,
            expiresAtRoomId, expiresAtCombatId,
            expiresAtFloorIndex);
    }

    public void Consume(DateTime consumedAt)
    {
        if (IsConsumed)
            return;

        ConsumedAtUtc = consumedAt;
    }
}
