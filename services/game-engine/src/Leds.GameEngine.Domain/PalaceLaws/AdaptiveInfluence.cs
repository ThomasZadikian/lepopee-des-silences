namespace Leds.GameEngine.Domain.PalaceLaws;

public sealed class AdaptiveInfluence
{
    private AdaptiveInfluence(
        Guid id,
        Guid runId,
        string sourceType,
        string sourceKey,
        string influenceType,
        string influenceTag,
        decimal? value,
        string? valueMode,
        string duration,
        DateTime createdAtUtc,
        DateTime? consumedAtUtc)
    {
        Id = id;
        RunId = runId;
        SourceType = sourceType;
        SourceKey = sourceKey;
        InfluenceType = influenceType;
        InfluenceTag = influenceTag;
        Value = value;
        ValueMode = valueMode;
        Duration = duration;
        CreatedAtUtc = createdAtUtc;
        ConsumedAtUtc = consumedAtUtc;
    }

    public Guid Id { get; }
    public Guid RunId { get; }
    public string SourceType { get; }
    public string SourceKey { get; }
    public string InfluenceType { get; }
    public string InfluenceTag { get; }
    public decimal? Value { get; }
    public string? ValueMode { get; }
    public string Duration { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public void Consume(DateTime consumedAt)
    {
        if (!IsConsumed)
            ConsumedAtUtc = consumedAt;
    }

    public static AdaptiveInfluence Create(
        Guid runId,
        string sourceType,
        string sourceKey,
        string influenceType,
        string influenceTag,
        decimal? value = null,
        string? valueMode = null,
        string duration = "UntilRunEnds")
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new Common.DomainException("Source type is required.");
        if (string.IsNullOrWhiteSpace(sourceKey))
            throw new Common.DomainException("Source key is required.");
        if (string.IsNullOrWhiteSpace(influenceType))
            throw new Common.DomainException("Influence type is required.");
        if (string.IsNullOrWhiteSpace(influenceTag))
            throw new Common.DomainException("Influence tag is required.");

        return new AdaptiveInfluence(
            Guid.NewGuid(),
            runId,
            sourceType.Trim(),
            sourceKey.Trim(),
            influenceType.Trim(),
            influenceTag.Trim(),
            value,
            valueMode?.Trim(),
            duration.Trim(),
            DateTime.UtcNow,
            null);
    }

    public static AdaptiveInfluence Rehydrate(
        Guid id,
        Guid runId,
        string sourceType,
        string sourceKey,
        string influenceType,
        string influenceTag,
        decimal? value,
        string? valueMode,
        string duration,
        DateTime createdAtUtc,
        DateTime? consumedAtUtc)
    {
        return new AdaptiveInfluence(
            id, runId, sourceType, sourceKey, influenceType,
            influenceTag, value, valueMode, duration, createdAtUtc, consumedAtUtc);
    }
}
