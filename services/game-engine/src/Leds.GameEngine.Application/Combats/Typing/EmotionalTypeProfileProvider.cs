using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Typing;

/// <summary>
/// Resolves a combatant's natural register, then derives all defensive affinities
/// from the single global matrix. Identity mappings are transitional snapshots of
/// Catalog definitions; they never redefine weakness, resistance or immunity.
/// </summary>
public sealed class EmotionalTypeProfileProvider : ICombatantTypeProfileProvider
{
    private static readonly IReadOnlyDictionary<string, CombatantTypeProfile> ProfilesByKey =
        new Dictionary<string, CombatantTypeProfile>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Hero (by SourceKey) ───────────────────────────────────────────
            // In combat the hero's SourceKey is the draft AllyKey "player.self";
            // "character.player.self" is the catalog/definition key. Both map here.
            ["player.self"] = Profile(EmotionalType.Memoire),
            ["character.player.self"] = Profile(EmotionalType.Memoire),
            ["character.thomas"] = Profile(EmotionalType.Silence),
            ["character.mane"] = Profile(EmotionalType.Rupture),
            ["character.mina"] = Profile(EmotionalType.Folie),
            ["character.elise"] = Profile(EmotionalType.Melancolie),
            ["character.john"] = Profile(EmotionalType.Deni),

            // ── Enemy archetypes (CatalogSeedRunner.UpsertEnemyAsync's `archetype`
            // param) — every distinct value actually used by the bestiary, plus
            // "Fragile" kept as a generic example/test fixture (not currently seeded). ──
            ["Fragile"] = Profile(EmotionalType.Melancolie),

            ["Shadow"] = Profile(EmotionalType.Effroi),

            ["Guard"] = Profile(EmotionalType.Rupture),

            ["Bruiser"] = Profile(EmotionalType.Rupture),

            ["Memory"] = Profile(EmotionalType.Memoire),

            ["Support"] = Profile(EmotionalType.Deni),

            ["Disruptor"] = Profile(EmotionalType.Folie),

            ["Skirmisher"] = Profile(EmotionalType.Silence),

            // Literal "Rupture" archetype (distinct role bucket from Guard/Bruiser,
            // both of which also attack with Rupture but carry different affinities).
            ["Rupture"] = Profile(EmotionalType.Rupture),

            // ── Named bosses (by SourceKey = EnemyDefinition.Key) — each gets a
            // bespoke profile instead of sharing the generic "Boss" archetype bucket.
            ["canon.enemy.grand-cardinal"] = Profile(EmotionalType.Deni),

            ["canon.enemy.imperatrice-vipere"] = Profile(EmotionalType.Folie),

            ["canon.enemy.homoncule-roi"] = Profile(EmotionalType.Rupture),

            ["canon.enemy.pape-louis-xvii"] = Profile(EmotionalType.Effroi),

            ["canon.enemy.himlit"] = Profile(EmotionalType.Folie),
        };

    public CombatantTypeProfile Resolve(Combatant combatant)
    {
        if (combatant is null)
        {
            return CombatantTypeProfile.Neutral;
        }

        var baseProfile = ResolveBaseProfile(combatant);

        // An item-driven attack type override changes the offensive type only;
        // the combatant keeps its innate weaknesses / resistances / immunities.
        if (combatant.AttackTypeOverride is { } overrideType && overrideType != baseProfile.AttackType)
        {
            return new CombatantTypeProfile(
                overrideType,
                baseProfile.WeakTo,
                baseProfile.ResistantTo,
                baseProfile.ImmuneTo);
        }

        return baseProfile;
    }

    private static CombatantTypeProfile ResolveBaseProfile(Combatant combatant)
    {
        if (!string.IsNullOrWhiteSpace(combatant.SourceKey)
            && ProfilesByKey.TryGetValue(combatant.SourceKey, out var bySource))
        {
            return bySource;
        }

        if (!string.IsNullOrWhiteSpace(combatant.Archetype)
            && ProfilesByKey.TryGetValue(combatant.Archetype, out var byArchetype))
        {
            return byArchetype;
        }

        return CombatantTypeProfile.Neutral;
    }

    public EmotionalType ResolveAttackType(Combatant attacker, CombatantSkill skill)
    {
        if (TryResolveIntrinsicType(skill, out var intrinsic))
        {
            return intrinsic;
        }

        // Default / basic attacks follow the caster's character/archetype type
        // (and can be modified by items).
        return Resolve(attacker).AttackType;
    }

    /// <summary>
    /// Resolves a skill's own emotional identity exclusively from the Catalog
    /// <c>EmotionalRegister</c> contract, independently
    /// of any caster. Used both by <see cref="ResolveAttackType"/> (which then falls back to
    /// the caster's type) and by <see cref="Dtos.CombatantSkillRuntimeDto.FromDomain"/> to
    /// surface a skill's "élément" badge in the UI for true spells (basic attacks correctly
    /// return false because Catalog explicitly declares them <c>Neutral</c>).
    /// </summary>
    public static bool TryResolveIntrinsicType(CombatantSkill? skill, out EmotionalType type)
        => TryResolveIntrinsicType(skill?.Key, skill?.Tags, skill?.EmotionalRegister, out type);

    /// <summary>
    /// Contract-oriented overload used by out-of-combat Catalog readers.
    /// The key and tags remain in the signature for contract compatibility but never
    /// participate in type resolution.
    /// </summary>
    public static bool TryResolveIntrinsicType(
        string? skillKey, IReadOnlyCollection<string>? tags, string? emotionalRegister, out EmotionalType type)
    {
        // Catalog is the sole authoring source. Tags and skill keys are intentionally
        // ignored: accepting them as overrides allowed contradictory definitions.
        var registerType = EmotionalTypeCode.ParseRequired(
            emotionalRegister,
            $"Skill '{skillKey ?? "<unknown>"}' EmotionalRegister");

        if (registerType != EmotionalType.Neutral)
        {
            type = registerType;
            return true;
        }

        type = default;
        return false;
    }

    private static CombatantTypeProfile Profile(EmotionalType naturalRegister)
    {
        var (weak, resistant, immune) = naturalRegister switch
        {
            EmotionalType.Effroi => (EmotionalType.Memoire, EmotionalType.Rupture, EmotionalType.Silence),
            EmotionalType.Deni => (EmotionalType.Melancolie, EmotionalType.Effroi, EmotionalType.Folie),
            EmotionalType.Melancolie => (EmotionalType.Silence, EmotionalType.Memoire, EmotionalType.Effroi),
            EmotionalType.Rupture => (EmotionalType.Folie, EmotionalType.Melancolie, EmotionalType.Deni),
            EmotionalType.Memoire => (EmotionalType.Deni, EmotionalType.Folie, EmotionalType.Rupture),
            EmotionalType.Silence => (EmotionalType.Rupture, EmotionalType.Deni, EmotionalType.Memoire),
            EmotionalType.Folie => (EmotionalType.Effroi, EmotionalType.Silence, EmotionalType.Melancolie),
            _ => (EmotionalType.Neutral, EmotionalType.Neutral, EmotionalType.Neutral)
        };

        return new CombatantTypeProfile(
            naturalRegister,
            weak == EmotionalType.Neutral ? new HashSet<EmotionalType>() : new HashSet<EmotionalType> { weak },
            resistant == EmotionalType.Neutral ? new HashSet<EmotionalType>() : new HashSet<EmotionalType> { resistant },
            immune == EmotionalType.Neutral ? new HashSet<EmotionalType>() : new HashSet<EmotionalType> { immune });
    }
}
