using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Domain.Gameplay;

/// <summary>
/// Canonical Catalog-owned vocabulary for emotional registers. External contracts
/// use <see cref="EmotionalRegisterDefinition.Code"/> and never enum ordinals.
/// </summary>
public static class EmotionalRegisterCatalog
{
    public const string Version = "emotional-registers-1.0.0";

    private static readonly IReadOnlyList<EmotionalRegisterDefinition> Definitions =
    [
        new("neutral", "Neutral", "·", "oklch(0.62 0.02 272)", EmotionalRegister.Neutral),
        new("effroi", "Effroi", "✶", "oklch(0.80 0.11 18)", EmotionalRegister.Effroi),
        new("deni", "Déni", "◇", "oklch(0.85 0.09 78)", EmotionalRegister.Deni),
        new("melancolie", "Mélancolie", "❍", "oklch(0.82 0.08 248)", EmotionalRegister.Melancolie),
        new("rupture", "Rupture", "⟡", "oklch(0.80 0.11 38)", EmotionalRegister.Rupture),
        new("memoire", "Mémoire", "◈", "oklch(0.88 0.08 86)", EmotionalRegister.Memoire),
        new("silence", "Silence", "○", "oklch(0.83 0.02 272)", EmotionalRegister.Silence),
        new("folie", "Folie", "✳", "oklch(0.80 0.13 340)", EmotionalRegister.Folie)
    ];

    private static readonly IReadOnlyDictionary<string, EmotionalRegisterDefinition> ByCode =
        Definitions.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<EmotionalRegister, EmotionalRegisterDefinition> ByValue =
        Definitions.ToDictionary(d => d.Value);

    public static IReadOnlyList<EmotionalRegisterDefinition> All => Definitions;

    public static IReadOnlyList<EmotionalRegisterDefinition> Active =>
        Definitions.Where(d => d.IsActive).ToArray();

    public static EmotionalRegister Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Emotional register is required.");

        var normalized = value.Trim();
        if (ByCode.TryGetValue(normalized, out var byCode))
            return byCode.Value;

        // Authoring may use enum-style names; every persisted/public value is normalized
        // back to the Catalog-owned stable code with CodeOf.
        var byName = Definitions.FirstOrDefault(d =>
            string.Equals(d.Value.ToString(), normalized, StringComparison.OrdinalIgnoreCase));

        return byName?.Value
            ?? throw new DomainException($"Unknown emotional register '{value}'.");
    }

    public static bool TryParse(string? value, out EmotionalRegister register)
    {
        try
        {
            register = Parse(value ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            register = default;
            return false;
        }
    }

    public static string CodeOf(EmotionalRegister register) =>
        ByValue.TryGetValue(register, out var definition)
            ? definition.Code
            : throw new DomainException($"Unknown emotional register value '{register}'.");
}

public sealed record EmotionalRegisterDefinition(
    string Code,
    string DisplayName,
    string Glyph,
    string Color,
    EmotionalRegister Value,
    bool IsActive = true);
