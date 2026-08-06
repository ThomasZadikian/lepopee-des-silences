using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Catalog;

public static class EnemyArchetypeCode
{
    private static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Beast", "Boss", "Bruiser", "Disruptor", "Elite", "Fragile", "Guard", "Memory",
        "Rupture", "Shadow", "Skirmisher", "Support", "Tank", "Trauma"
    };

    public static string ParseRequired(string? value, string contractField)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{contractField} is required.");

        return Codes.FirstOrDefault(code =>
            string.Equals(code, value.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                $"{contractField} contains unknown archetype '{value}'.");
    }
}
