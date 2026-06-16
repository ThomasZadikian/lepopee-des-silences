namespace Leds.GameEngine.Domain.Selection;

public sealed class MarkovSelectionInfluence
{
    public MarkovSelectionInfluence(
        string matrixVersion,
        string influenceTag,
        IReadOnlyDictionary<string, decimal> candidateWeightModifiers)
    {
        if (string.IsNullOrWhiteSpace(matrixVersion))
            throw new Common.DomainException("Matrix version is required.");

        MatrixVersion = matrixVersion.Trim();
        InfluenceTag = influenceTag?.Trim() ?? "default";
        CandidateWeightModifiers = candidateWeightModifiers ?? new Dictionary<string, decimal>();
    }

    public string MatrixVersion { get; }
    public string InfluenceTag { get; }
    public IReadOnlyDictionary<string, decimal> CandidateWeightModifiers { get; }

    public decimal GetModifier(string candidateKey)
    {
        return CandidateWeightModifiers.TryGetValue(candidateKey, out var modifier)
            ? modifier
            : 1.0m;
    }
}
