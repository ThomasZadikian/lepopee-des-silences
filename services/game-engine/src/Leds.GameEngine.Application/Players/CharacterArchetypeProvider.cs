namespace Leds.GameEngine.Application.Players;

/// <summary>
/// Resolves a player character's combat archetype from its catalog
/// <c>DefinitionKey</c>. Mirrors <c>EmotionalTypeProfileProvider.ProfilesByKey</c>: a
/// lightweight beta tuning table, promotable to catalog/seed later without touching
/// the equip-validation logic that consumes it (see <see cref="SkillArchetypeGate"/>).
/// </summary>
public static class CharacterArchetypeProvider
{
    /// <summary>
    /// "L'Aventurier" (the protagonist) is a blank-slate archetype by design — the
    /// player builds their own identity rather than inheriting a companion's fixed
    /// role, so no skill's AllowedArchetypes list can ever exclude them.
    /// </summary>
    public const string Adaptive = "Adaptive";

    public const string Tank = "Tank";
    public const string GlassCannon = "GlassCannon";
    public const string Support = "Support";
    public const string Hybrid = "Hybrid";
    public const string Opportunist = "Opportunist";

    private static readonly IReadOnlyDictionary<string, string> ArchetypesByDefinitionKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["character.player.self"] = Adaptive,
            ["character.thomas"] = Tank,
            ["character.mane"] = GlassCannon,
            ["character.mina"] = Support,
            ["character.elise"] = Hybrid,
            ["character.john"] = Opportunist,
        };

    /// <summary>Unknown/future characters default to Adaptive rather than being
    /// silently locked out of every restricted skill.</summary>
    public static string Resolve(string? characterDefinitionKey)
    {
        if (!string.IsNullOrWhiteSpace(characterDefinitionKey)
            && ArchetypesByDefinitionKey.TryGetValue(characterDefinitionKey, out var archetype))
        {
            return archetype;
        }

        return Adaptive;
    }
}
