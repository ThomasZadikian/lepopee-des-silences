using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Canonical character-register contract used while composing an immutable run snapshot.
/// This mapping mirrors the versioned Catalog definitions and is deliberately evaluated
/// once at run start; combat code must consume the snapshotted code, never the character key.
/// </summary>
public static class CharacterEmotionalRegisterCode
{
    private static readonly IReadOnlyDictionary<string, string> ByDefinitionKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["character.player.self"] = "memoire",
            ["character.thomas"] = "silence",
            ["character.mane"] = "rupture",
            ["character.mina"] = "folie",
            ["character.elise"] = "melancolie",
            ["character.john"] = "deni"
        };

    public static string ResolveRequired(string definitionKey)
    {
        if (string.IsNullOrWhiteSpace(definitionKey)
            || !ByDefinitionKey.TryGetValue(definitionKey.Trim(), out var code))
        {
            throw new DomainException(
                $"Character definition '{definitionKey}' has no canonical emotional register.");
        }

        return code;
    }
}
