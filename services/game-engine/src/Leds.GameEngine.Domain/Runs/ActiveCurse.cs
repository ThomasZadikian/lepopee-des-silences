using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class ActiveCurse
{
    private ActiveCurse(
        Guid id,
        string key,
        string displayName,
        string description,
        double difficultyDelta,
        DateTime appliedAtUtc,
        string? curseDefinitionKey,
        int severity,
        string duration,
        Guid? expiresAtRoomId,
        DateTime? consumedAtUtc,
        string? effectSetKey)
    {
        Id = id;
        Key = key;
        DisplayName = displayName;
        Description = description;
        DifficultyDelta = difficultyDelta;
        AppliedAtUtc = appliedAtUtc;
        CurseDefinitionKey = curseDefinitionKey;
        Severity = severity;
        Duration = duration;
        ExpiresAtRoomId = expiresAtRoomId;
        ConsumedAtUtc = consumedAtUtc;
        EffectSetKey = effectSetKey;
    }

    public Guid Id { get; }
    public string Key { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public double DifficultyDelta { get; }
    public DateTime AppliedAtUtc { get; }
    public string? CurseDefinitionKey { get; }
    public int Severity { get; }
    public string Duration { get; }
    public Guid? ExpiresAtRoomId { get; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public string? EffectSetKey { get; }
    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public void Consume(DateTime consumedAt)
    {
        if (!IsConsumed)
            ConsumedAtUtc = consumedAt;
    }

    public static ActiveCurse Create(
        string key,
        string displayName,
        string? description,
        double difficultyDelta,
        DateTime appliedAtUtc,
        string? curseDefinitionKey = null,
        int severity = 3,
        string? duration = null,
        string? effectSetKey = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Curse key is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Curse display name is required.");
        if (difficultyDelta is < -0.5 or > 0.5)
            throw new DomainException("Curse difficulty delta must be between -0.5 and 0.5.");

        return new ActiveCurse(
            Guid.NewGuid(),
            key.Trim(),
            displayName.Trim(),
            description?.Trim() ?? string.Empty,
            difficultyDelta,
            appliedAtUtc,
            curseDefinitionKey?.Trim(),
            severity,
            duration ?? "NextCombatOnly",
            expiresAtRoomId: null,
            consumedAtUtc: null,
            effectSetKey?.Trim());
    }

    public static ActiveCurse Rehydrate(
        Guid id,
        string key,
        string displayName,
        string description,
        double difficultyDelta,
        DateTime appliedAtUtc,
        string? curseDefinitionKey = null,
        int severity = 3,
        string? duration = null,
        Guid? expiresAtRoomId = null,
        DateTime? consumedAtUtc = null,
        string? effectSetKey = null)
    {
        return new ActiveCurse(
            id, key, displayName, description, difficultyDelta, appliedAtUtc,
            curseDefinitionKey, severity, duration ?? "NextCombatOnly",
            expiresAtRoomId, consumedAtUtc, effectSetKey);
    }
}
