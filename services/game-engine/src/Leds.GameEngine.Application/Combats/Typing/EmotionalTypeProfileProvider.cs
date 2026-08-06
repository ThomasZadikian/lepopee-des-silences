using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Typing;

/// <summary>
/// Derives every defensive affinity from the combatant's snapshotted natural register.
/// Character keys and tactical archetypes never participate in affinity resolution.
/// </summary>
public sealed class EmotionalTypeProfileProvider : ICombatantTypeProfileProvider
{
    public CombatantTypeProfile Resolve(Combatant combatant)
    {
        if (combatant is null)
        {
            return CombatantTypeProfile.Neutral;
        }

        var baseProfile = Profile(combatant.NaturalEmotionalType);

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
        if (naturalRegister == EmotionalType.Neutral)
        {
            return CombatantTypeProfile.Neutral;
        }

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
