using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// Boundary parser for Catalog-owned emotional register codes. Enum numeric values
/// never cross service boundaries; only stable, case-insensitive names are accepted.
/// </summary>
public static class EmotionalTypeCode
{
    public static EmotionalType ParseRequired(string? code, string contractField)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException($"{contractField} is required.");

        if (!Enum.TryParse<EmotionalType>(code.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException(
                $"{contractField} contains unsupported emotional register '{code}'.");
        }

        return parsed;
    }

    public static string NormalizeRequired(string? code, string contractField) =>
        ParseRequired(code, contractField).ToString().ToLowerInvariant();
}
