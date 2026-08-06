using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Enemies;

/// <summary>Canonical tactical archetype codes accepted by published enemy definitions.</summary>
public static class EnemyArchetypeCatalog
{
    private static readonly IReadOnlySet<string> Codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Beast", "Boss", "Bruiser", "Disruptor", "Elite", "Fragile", "Guard", "Memory",
        "Rupture", "Shadow", "Skirmisher", "Support", "Tank", "Trauma"
    };

    public static IReadOnlyCollection<string> All => Codes
        .Order(StringComparer.Ordinal)
        .ToArray();

    public static string Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Enemy definition archetype is required.");

        var normalized = Codes.FirstOrDefault(code =>
            string.Equals(code, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized
            ?? throw new DomainException(
                $"Unknown enemy archetype '{value}'. Expected one of: {string.Join(", ", Codes.Order())}.");
    }
}
