namespace Leds.GameEngine.Domain.Markov;

/// <summary>
/// Represents a finite state in a Markov chain.
/// Equality is case-insensitive to avoid duplicate logical states such as "Wary" and "wary".
/// </summary>
public sealed class MarkovState : IEquatable<MarkovState>
{
    private MarkovState(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MarkovState Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MarkovMatrixValidationException("Markov state value is required.");
        }

        return new MarkovState(value.Trim());
    }

    public bool Equals(MarkovState? other)
    {
        return other is not null
            && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is MarkovState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}