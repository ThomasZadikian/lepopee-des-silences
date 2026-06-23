namespace Leds.GameEngine.Domain.Combats.Typing;

/// <summary>
/// A combatant's emotional type identity: the type its attacks deal by default,
/// and the types it is weak to / resistant to / immune to. The same shape is
/// used for enemies and heroes (symmetric design).
/// <para>
/// <see cref="Neutral"/> is the inert profile: Neutral attack type and no
/// affinities, so an unknown combatant neither exploits nor suffers the type
/// system (always ×1.0).
/// </para>
/// </summary>
public sealed class CombatantTypeProfile
{
    /// <summary>The inert profile used for any combatant without declared types.</summary>
    public static readonly CombatantTypeProfile Neutral = new(
        EmotionalType.Neutral,
        new HashSet<EmotionalType>(),
        new HashSet<EmotionalType>(),
        new HashSet<EmotionalType>());

    public CombatantTypeProfile(
        EmotionalType attackType,
        IReadOnlySet<EmotionalType> weakTo,
        IReadOnlySet<EmotionalType> resistantTo,
        IReadOnlySet<EmotionalType> immuneTo)
    {
        AttackType = attackType;
        WeakTo = weakTo;
        ResistantTo = resistantTo;
        ImmuneTo = immuneTo;
    }

    /// <summary>The type this combatant's damaging skills deal when a skill declares none itself.</summary>
    public EmotionalType AttackType { get; }

    public IReadOnlySet<EmotionalType> WeakTo { get; }
    public IReadOnlySet<EmotionalType> ResistantTo { get; }
    public IReadOnlySet<EmotionalType> ImmuneTo { get; }

    /// <summary>
    /// Resolves how effective an incoming attack type is against this profile.
    /// Immunity wins over weakness, weakness over resistance; a Neutral incoming
    /// type is always neutral.
    /// </summary>
    public DamageEffectiveness EffectivenessAgainst(EmotionalType incoming)
    {
        if (incoming == EmotionalType.Neutral)
        {
            return DamageEffectiveness.Neutral;
        }

        if (ImmuneTo.Contains(incoming))
        {
            return DamageEffectiveness.Immune;
        }

        if (WeakTo.Contains(incoming))
        {
            return DamageEffectiveness.Weak;
        }

        if (ResistantTo.Contains(incoming))
        {
            return DamageEffectiveness.Resistant;
        }

        return DamageEffectiveness.Neutral;
    }
}