namespace Leds.GameEngine.Domain.Selection;

public sealed class SelectionDecision
{
    private SelectionDecision(
        Guid id,
        Guid runId,
        string decisionType,
        string? contextKey,
        string selectedKey,
        string? selectionGroup,
        string seed,
        string algorithmVersion,
        DateTime createdAtUtc,
        string? matrixVersion,
        string? influenceSummaryKey)
    {
        Id = id;
        RunId = runId;
        DecisionType = decisionType;
        ContextKey = contextKey;
        SelectedKey = selectedKey;
        SelectionGroup = selectionGroup;
        Seed = seed;
        AlgorithmVersion = algorithmVersion;
        CreatedAtUtc = createdAtUtc;
        MatrixVersion = matrixVersion;
        InfluenceSummaryKey = influenceSummaryKey;
    }

    public Guid Id { get; }
    public Guid RunId { get; }
    public string DecisionType { get; }
    public string? ContextKey { get; }
    public string SelectedKey { get; }
    public string? SelectionGroup { get; }
    public string Seed { get; }
    public string AlgorithmVersion { get; }
    public DateTime CreatedAtUtc { get; }
    public string? MatrixVersion { get; }
    public string? InfluenceSummaryKey { get; }

    public static SelectionDecision Create(
        Guid runId,
        string decisionType,
        string? contextKey,
        string selectedKey,
        string? selectionGroup,
        string seed,
        string algorithmVersion,
        string? matrixVersion = null,
        string? influenceSummaryKey = null)
    {
        if (string.IsNullOrWhiteSpace(decisionType))
            throw new Common.DomainException("Selection decision type is required.");
        if (string.IsNullOrWhiteSpace(selectedKey))
            throw new Common.DomainException("Selection decision selected key is required.");
        if (string.IsNullOrWhiteSpace(seed))
            throw new Common.DomainException("Selection decision seed is required.");

        return new SelectionDecision(
            Guid.NewGuid(),
            runId,
            decisionType.Trim(),
            contextKey?.Trim(),
            selectedKey.Trim(),
            selectionGroup?.Trim(),
            seed.Trim(),
            algorithmVersion.Trim(),
            DateTime.UtcNow,
            matrixVersion?.Trim(),
            influenceSummaryKey?.Trim());
    }

    public static SelectionDecision Rehydrate(
        Guid id,
        Guid runId,
        string decisionType,
        string? contextKey,
        string selectedKey,
        string? selectionGroup,
        string seed,
        string algorithmVersion,
        DateTime createdAtUtc,
        string? matrixVersion = null,
        string? influenceSummaryKey = null)
    {
        return new SelectionDecision(
            id, runId, decisionType, contextKey, selectedKey,
            selectionGroup, seed, algorithmVersion, createdAtUtc,
            matrixVersion, influenceSummaryKey);
    }
}
