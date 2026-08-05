using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Typing;

/// <summary>
/// Default emotional type provider, backed by an in-engine affinity table keyed
/// by <see cref="Combatant.SourceKey"/> first, then <see cref="Combatant.Archetype"/>.
/// Unknown combatants resolve to <see cref="CombatantTypeProfile.Neutral"/>, so the
/// type system is inert for anything not yet declared.
/// <para>
/// This table is the single place to tune type/weakness data for the beta; it is
/// promotable to catalog/seed without touching the combat math.
/// </para>
/// </summary>
public sealed class EmotionalTypeProfileProvider : ICombatantTypeProfileProvider
{
    private const string TypeTagPrefix = "emotype:";

    /// <summary>
    /// Intrinsic emotional type of a spell, keyed by skill key. A spell's type
    /// belongs to the spell itself, not to the caster, so it is resolved here
    /// regardless of who casts it.
    /// <para>
    /// Basic / default attacks (e.g. <c>skill.basic.strike</c>) are intentionally
    /// ABSENT: they follow the caster's character/archetype type and stay
    /// modifiable by items. Only declare entries here for true spells.
    /// </para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, EmotionalType> SkillTypesByKey =
        new Dictionary<string, EmotionalType>(StringComparer.OrdinalIgnoreCase)
        {
            // Example (reward skill, not yet equippable) — tune the type freely.
            ["skill-shadow-bite"] = EmotionalType.Silence,
            // Ethan's "Frayeur organique" (canon.skill.frayeur-organique): the dread
            // it inflicts is the spell's own nature, independent of who casts it.
            ["canon.skill.frayeur-organique"] = EmotionalType.Effroi,
            ["canon.skill.sursaut-memoriel"] = EmotionalType.Memoire,
            ["canon.skill.anagramme"] = EmotionalType.Deni,
            ["canon.skill.lecture-des-silences"] = EmotionalType.Silence,
            ["canon.skill.nevrose"] = EmotionalType.Effroi,
            ["canon.skill.plongee-dans-la-folie"] = EmotionalType.Folie,
        };

    private static readonly IReadOnlyDictionary<string, CombatantTypeProfile> ProfilesByKey =
        new Dictionary<string, CombatantTypeProfile>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Hero (by SourceKey) ───────────────────────────────────────────
            // In combat the hero's SourceKey is the draft AllyKey "player.self";
            // "character.player.self" is the catalog/definition key. Both map here.
            ["player.self"] = Profile(
                EmotionalType.Memoire,
                weak: [EmotionalType.Effroi],
                resist: [EmotionalType.Silence]),
            ["character.player.self"] = Profile(
                EmotionalType.Memoire,
                weak: [EmotionalType.Effroi],
                resist: [EmotionalType.Silence]),

            // ── Enemy archetypes (CatalogSeedRunner.UpsertEnemyAsync's `archetype`
            // param) — every distinct value actually used by the bestiary, plus
            // "Fragile" kept as a generic example/test fixture (not currently seeded). ──
            ["Fragile"] = Profile(
                EmotionalType.Melancolie,
                weak: [EmotionalType.Rupture],
                resist: [EmotionalType.Melancolie]),

            ["Shadow"] = Profile(
                EmotionalType.Effroi,
                weak: [EmotionalType.Silence],
                resist: [EmotionalType.Deni]),

            ["Guard"] = Profile(
                EmotionalType.Rupture,
                weak: [EmotionalType.Deni],
                resist: [EmotionalType.Rupture]),

            ["Bruiser"] = Profile(
                EmotionalType.Rupture,
                weak: [EmotionalType.Melancolie],
                resist: [EmotionalType.Effroi]),

            ["Memory"] = Profile(
                EmotionalType.Memoire,
                weak: [EmotionalType.Rupture],
                resist: [EmotionalType.Silence]),

            ["Support"] = Profile(
                EmotionalType.Deni,
                weak: [EmotionalType.Melancolie],
                resist: [EmotionalType.Folie]),

            ["Disruptor"] = Profile(
                EmotionalType.Folie,
                weak: [EmotionalType.Memoire],
                resist: [EmotionalType.Deni]),

            ["Skirmisher"] = Profile(
                EmotionalType.Silence,
                weak: [EmotionalType.Folie],
                resist: [EmotionalType.Rupture]),

            // Literal "Rupture" archetype (distinct role bucket from Guard/Bruiser,
            // both of which also attack with Rupture but carry different affinities).
            ["Rupture"] = Profile(
                EmotionalType.Rupture,
                weak: [EmotionalType.Memoire],
                resist: [EmotionalType.Deni]),

            // ── Named bosses (by SourceKey = EnemyDefinition.Key) — each gets a
            // bespoke profile instead of sharing the generic "Boss" archetype bucket.
            ["canon.enemy.grand-cardinal"] = Profile(
                EmotionalType.Deni,
                weak: [EmotionalType.Folie],
                resist: [EmotionalType.Melancolie]),

            ["canon.enemy.imperatrice-vipere"] = Profile(
                EmotionalType.Folie,
                weak: [EmotionalType.Memoire],
                resist: [EmotionalType.Rupture]),

            ["canon.enemy.homoncule-roi"] = Profile(
                EmotionalType.Rupture,
                weak: [EmotionalType.Silence],
                resist: [EmotionalType.Deni]),

            ["canon.enemy.pape-louis-xvii"] = Profile(
                EmotionalType.Effroi,
                weak: [EmotionalType.Deni],
                resist: [EmotionalType.Silence]),

            ["canon.enemy.himlit"] = Profile(
                EmotionalType.Folie,
                weak: [EmotionalType.Memoire],
                resist: [EmotionalType.Effroi, EmotionalType.Silence]),
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
    /// Resolves a skill's OWN elemental identity — a tag override, its entry in
    /// <see cref="SkillTypesByKey"/>, or its catalog <c>EmotionalRegister</c> — independently
    /// of any caster. Used both by <see cref="ResolveAttackType"/> (which then falls back to
    /// the caster's type) and by <see cref="Dtos.CombatantSkillRuntimeDto.FromDomain"/> to
    /// surface a skill's "élément" badge in the UI for true spells (basic attacks correctly
    /// return false — see the doc comment on <see cref="SkillTypesByKey"/>).
    /// </summary>
    public static bool TryResolveIntrinsicType(CombatantSkill? skill, out EmotionalType type)
        => TryResolveIntrinsicType(skill?.Key, skill?.Tags, skill?.EmotionalRegister, out type);

    /// <summary>
    /// Key/tags-only overload of <see cref="TryResolveIntrinsicType(CombatantSkill?, out EmotionalType)"/>,
    /// so out-of-combat catalog readers (e.g. the Grimoire's skill list) can resolve the exact
    /// same "élément" without needing a runtime <see cref="CombatantSkill"/> instance.
    /// </summary>
    public static bool TryResolveIntrinsicType(
        string? skillKey, IReadOnlyCollection<string>? tags, string? emotionalRegister, out EmotionalType type)
    {
        // 1) Explicit per-skill override via a tag, e.g. "emotype:rupture".
        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                var trimmed = tag.Trim();
                if (trimmed.StartsWith(TypeTagPrefix, StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<EmotionalType>(trimmed[TypeTagPrefix.Length..], ignoreCase: true, out var parsed))
                {
                    type = parsed;
                    return true;
                }
            }
        }

        // 2) A handful of pre-catalog special cases (kept for skills authored before the
        // EmotionalRegister column existed / not seeded through UpsertSkillAsync).
        if (skillKey is not null && SkillTypesByKey.TryGetValue(skillKey, out var spellType))
        {
            type = spellType;
            return true;
        }

        // 3) The skill's own catalog EmotionalRegister — the primary, per-skill source of
        // truth now that every seeded skill declares one (see CatalogSeedRunner).
        if (!string.IsNullOrWhiteSpace(emotionalRegister)
            && !string.Equals(emotionalRegister, "Neutral", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<EmotionalType>(emotionalRegister, ignoreCase: true, out var registerType))
        {
            type = registerType;
            return true;
        }

        type = default;
        return false;
    }

    private static CombatantTypeProfile Profile(
        EmotionalType attack,
        EmotionalType[]? weak = null,
        EmotionalType[]? resist = null,
        EmotionalType[]? immune = null)
    {
        return new CombatantTypeProfile(
            attack,
            new HashSet<EmotionalType>(weak ?? []),
            new HashSet<EmotionalType>(resist ?? []),
            new HashSet<EmotionalType>(immune ?? []));
    }
}