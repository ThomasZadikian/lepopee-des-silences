namespace Leds.GameEngine.Domain.Selection;

public sealed class SelectionCandidate
{
    public SelectionCandidate(
        string key,
        int weight,
        string? selectionGroup = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new Common.DomainException("Selection candidate key is required.");
        if (weight < 0)
            throw new Common.DomainException("Selection candidate weight must be non-negative.");

        Key = key.Trim();
        Weight = weight;
        SelectionGroup = selectionGroup?.Trim();
    }

    public string Key { get; }
    public int Weight { get; }
    public string? SelectionGroup { get; }
}
