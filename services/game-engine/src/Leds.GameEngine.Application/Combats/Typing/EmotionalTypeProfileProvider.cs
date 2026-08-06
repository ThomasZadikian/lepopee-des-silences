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

        var baseProfile = Profile(combatant.NaturalEmotionalType, emotionalAffinityMatrix);

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

    private static CombatantTypeProfile Profile(
        EmotionalType naturalRegister,
        EmotionalAffinityMatrixSnapshot emotionalAffinityMatrix)
    {
        if (naturalRegister == EmotionalType.Neutral)
            return CombatantTypeProfile.Neutral;

        return new CombatantTypeProfile(
            naturalRegister,
            ResolveIncoming(DamageEffectiveness.Weak),
            ResolveIncoming(DamageEffectiveness.Resistant),
            ResolveIncoming(DamageEffectiveness.Immune));

        IReadOnlySet<EmotionalType> ResolveIncoming(DamageEffectiveness effectiveness) =>
            Enum.GetValues<EmotionalType>()
                .Where(attack => emotionalAffinityMatrix.Resolve(attack, naturalRegister) == effectiveness)
                .ToHashSet();
    }
}
