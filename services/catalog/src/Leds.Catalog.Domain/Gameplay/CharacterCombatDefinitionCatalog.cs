using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Domain.Gameplay;

public enum CharacterKind
{
    Protagonist = 0,
    Companion = 1
}

/// <summary>
/// Catalog-owned combat identity for authored characters. Instance ids remain
/// owned by Player; consumers resolve immutable content through DefinitionKey.
/// </summary>
public sealed record CharacterCombatDefinition(
    string DefinitionKey,
    CharacterKind Kind,
    string CombatArchetypeCode,
    EmotionalRegister EmotionalRegister);

public static class CharacterCombatDefinitionCatalog
{
    private static readonly IReadOnlyDictionary<string, CharacterCombatDefinition> ByKey =
        new[]
        {
            new CharacterCombatDefinition("character.player.self", CharacterKind.Protagonist, "Adaptive", EmotionalRegister.Memoire),
            new CharacterCombatDefinition("character.thomas", CharacterKind.Companion, "Tank", EmotionalRegister.Silence),
            new CharacterCombatDefinition("character.mane", CharacterKind.Companion, "GlassCannon", EmotionalRegister.Rupture),
            new CharacterCombatDefinition("character.mina", CharacterKind.Companion, "Support", EmotionalRegister.Folie),
            new CharacterCombatDefinition("character.elise", CharacterKind.Companion, "Hybrid", EmotionalRegister.Melancolie),
            new CharacterCombatDefinition("character.john", CharacterKind.Companion, "Opportunist", EmotionalRegister.Deni)
        }.ToDictionary(definition => definition.DefinitionKey, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<CharacterCombatDefinition> All => ByKey.Values.ToArray();

    public static CharacterCombatDefinition GetRequired(string definitionKey)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
            throw new DomainException("Character definition key is required.");

        return ByKey.TryGetValue(definitionKey.Trim(), out var definition)
            ? definition
            : throw new DomainException($"Unknown character combat definition '{definitionKey}'.");
    }
}
