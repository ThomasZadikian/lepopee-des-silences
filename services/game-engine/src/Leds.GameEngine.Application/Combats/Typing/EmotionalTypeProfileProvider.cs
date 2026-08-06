using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Typing;

/// <summary>
/// Derives every defensive affinity from the combatant's snapshotted natural register.
/// Character keys and tactical archetypes never participate in affinity resolution.
/// </summary>
public sealed class EmotionalTypeProfileProvider : ICombatantTypeProfileProvider
{
    public CombatantTypeProfile Resolve(
        Combatant combatant,
        EmotionalAffinityMatrixSnapshot emotionalAffinityMatrix)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        ArgumentNullException.ThrowIfNull(emotionalAffinityMatrix);

        var baseProfile = Profile(
            combatant.NaturalEmotionalType,
            emotionalAffinityMatrix,
            combatant.EmotionalAffinityModifiers);

        // An item-driven attack type override changes the offensive type only;
        // the combatant keeps its innate weaknesses / resistances / immunities.
        if (combatant.AttackTypeOverride is { } overrideType && overrideType != baseProfile.AttackType)
        {
            return new CombatantTypeProfile(
                overrideType,
                baseProfile.BaseAffinities,
                combatant.EmotionalAffinityModifiers);
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
        return attacker.AttackTypeOverride ?? attacker.NaturalEmotionalType;
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
    {
        var registerType = EmotionalTypeCode.ParseRequired(
            skill?.EmotionalRegister,
            $"Skill '{skill?.Key ?? "<unknown>"}' EmotionalRegister");

        if (registerType != EmotionalType.Neutral)
        {
            type = registerType;
            return true;
        }

        type = default;
        return false;
    }

    private static CombatantTypeProfile Profile(
        EmotionalType naturalRegister,
        EmotionalAffinityMatrixSnapshot emotionalAffinityMatrix,
        IReadOnlyCollection<EmotionalAffinityModifier> modifiers)
    {
        return new CombatantTypeProfile(
            naturalRegister,
            ResolveAffinities(),
            modifiers);

        IReadOnlyDictionary<EmotionalType, BaseEmotionalAffinity> ResolveAffinities() =>
            Enum.GetValues<EmotionalType>().ToDictionary(
                attack => attack,
                attack => new BaseEmotionalAffinity(
                    emotionalAffinityMatrix.Resolve(attack, naturalRegister),
                    emotionalAffinityMatrix.ResolveMultiplier(attack, naturalRegister)));
    }
}
