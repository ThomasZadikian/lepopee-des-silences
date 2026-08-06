using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Domain.Gameplay;

/// <summary>
/// Canonical Catalog-owned vocabulary for emotional registers. External contracts
/// use <see cref="EmotionalRegisterDefinition.Code"/> and never enum ordinals.
/// </summary>
public static class EmotionalRegisterCatalog
{
    private static readonly IReadOnlyList<EmotionalRegisterDefinition> Definitions =
    [
        new("neutral", "Neutral", EmotionalRegister.Neutral),
        new("effroi", "Effroi", EmotionalRegister.Effroi),
        new("deni", "Déni", EmotionalRegister.Deni),
        new("melancolie", "Mélancolie", EmotionalRegister.Melancolie),
        new("rupture", "Rupture", EmotionalRegister.Rupture),
        new("memoire", "Mémoire", EmotionalRegister.Memoire),
        new("silence", "Silence", EmotionalRegister.Silence),
        new("folie", "Folie", EmotionalRegister.Folie)
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

        // Compatibility for existing internal snapshots authored with enum names.
        // The canonical value returned on contracts remains the stable lower-case code.
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
    EmotionalRegister Value,
    bool IsActive = true);
