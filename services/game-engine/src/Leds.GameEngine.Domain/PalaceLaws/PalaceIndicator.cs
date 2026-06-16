namespace Leds.GameEngine.Domain.PalaceLaws;

public sealed class PalaceIndicator
{
    private PalaceIndicator(
        Guid id,
        Guid runId,
        string indicatorKey,
        string displayLabel,
        string narrativeText,
        string intensity,
        Guid? sourceDecisionId,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc)
    {
        Id = id;
        RunId = runId;
        IndicatorKey = indicatorKey;
        DisplayLabel = displayLabel;
        NarrativeText = narrativeText;
        Intensity = intensity;
        SourceDecisionId = sourceDecisionId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; }
    public Guid RunId { get; }
    public string IndicatorKey { get; }
    public string DisplayLabel { get; }
    public string NarrativeText { get; }
    public string Intensity { get; }
    public Guid? SourceDecisionId { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? ExpiresAtUtc { get; }
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTime.UtcNow;

    public static PalaceIndicator Create(
        Guid runId,
        string indicatorKey,
        string displayLabel,
        string narrativeText,
        string intensity,
        Guid? sourceDecisionId = null,
        DateTime? expiresAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(indicatorKey))
            throw new Common.DomainException("Indicator key is required.");
        if (string.IsNullOrWhiteSpace(displayLabel))
            throw new Common.DomainException("Display label is required.");
        if (string.IsNullOrWhiteSpace(narrativeText))
            throw new Common.DomainException("Narrative text is required.");
        if (string.IsNullOrWhiteSpace(intensity))
            throw new Common.DomainException("Intensity is required.");

        return new PalaceIndicator(
            Guid.NewGuid(),
            runId,
            indicatorKey.Trim(),
            displayLabel.Trim(),
            narrativeText.Trim(),
            intensity.Trim(),
            sourceDecisionId,
            DateTime.UtcNow,
            expiresAtUtc);
    }

    public static PalaceIndicator Rehydrate(
        Guid id,
        Guid runId,
        string indicatorKey,
        string displayLabel,
        string narrativeText,
        string intensity,
        Guid? sourceDecisionId,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc)
    {
        return new PalaceIndicator(
            id, runId, indicatorKey, displayLabel, narrativeText,
            intensity, sourceDecisionId, createdAtUtc, expiresAtUtc);
    }
}
