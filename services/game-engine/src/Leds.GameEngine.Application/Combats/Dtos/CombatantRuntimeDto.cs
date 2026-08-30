using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatantRuntimeDto(
    Guid Id,
    string SourceKey,
    string SourceDefinitionKey,
    Guid? CharacterInstanceId,
    string DisplayName,
    string Side,
    string Archetype,
    int MaxVitality,
    int CurrentVitality,
    int Guard,
    int Mana,
    int MaxMana,
    decimal Charge,
    string Status,
    string NaturalEmotionalRegister,
    string EffectiveAttackRegister,
    IReadOnlyCollection<ResolvedEmotionalAffinityDto> IncomingAffinities,
    int AttackPower,
    int Defense,
    int Speed,
    int Focus,
    int MagicAttack,
    int MagicDefense,
    double ThreatValue,
    IReadOnlyCollection<CombatantStatusEffectDto> StatusEffects,
    IReadOnlyCollection<CombatantSkillRuntimeDto> Skills,
    int HitChanceBonusPercent = 0,
    int Evasion = 0,
    int CriticalChanceBonusPercent = 0)
{
    // Stateless pure provider; safe to share. Resolves the emotional type and
    // affinities (honours an item-driven AttackTypeOverride on the combatant).
    private static readonly EmotionalTypeProfileProvider TypeProvider = new();

    public static CombatantRuntimeDto FromDomain(
        Combatant combatant,
        int currentTick,
        EmotionalAffinityMatrixSnapshot emotionalAffinityMatrix)
    {
        var profile = TypeProvider.Resolve(
            combatant,
            emotionalAffinityMatrix);

        return new CombatantRuntimeDto(
            Id: combatant.Id.Value,
            SourceKey: combatant.SourceKey,
            SourceDefinitionKey: combatant.SourceDefinitionKey,
            CharacterInstanceId: combatant.CharacterInstanceId,
            DisplayName: combatant.DisplayName,
            Side: combatant.Side.ToString(),
            Archetype: combatant.Archetype,
            MaxVitality: combatant.MaxVitality,
            CurrentVitality: combatant.CurrentVitality,
            Guard: combatant.Guard,
            Mana: combatant.Mana,
            MaxMana: combatant.MaxMana,
            Charge: combatant.Charge,
            Status: combatant.Status.ToString(),
            NaturalEmotionalRegister: CodeOf(combatant.NaturalEmotionalType),
            EffectiveAttackRegister: CodeOf(profile.AttackType),
            IncomingAffinities: Enum.GetValues<EmotionalType>()
                .Select(register => ResolveIncomingAffinity(profile, register))
                .ToArray(),
            // Effective values so active buffs/debuffs are visible in the UI.
            AttackPower: combatant.EffectiveAttackPower,
            Defense: combatant.EffectiveDefense,
            Speed: combatant.EffectiveSpeed,
            Focus: combatant.EffectiveFocus,
            MagicAttack: combatant.EffectiveMagicAttack,
            MagicDefense: combatant.EffectiveMagicDefense,
            ThreatValue: combatant.ThreatValue,
            HitChanceBonusPercent: combatant.HitChanceBonusPercent,
            Evasion: combatant.EffectiveEvasion,
            CriticalChanceBonusPercent: combatant.EffectiveCriticalChanceBonusPercent,
            StatusEffects: combatant.StatusEffects
                .Select(e => new CombatantStatusEffectDto(
                    e.Key,
                    e.DisplayName,
                    e.Kind.ToString(),
                    e.Stat.ToString(),
                    e.Magnitude,
                    e.Stacks,
                    e.IsMagnitudePercentOfBaseStat,
                    e.PeekPerTickAmount(combatant.MaxVitality),
                    e.IsPermanent ? null : Math.Max(0, e.ExpiresAtTick - currentTick),
                    e.IsPermanent))
                .ToArray(),
            Skills: combatant.Skills
                .Select(skill => CombatantSkillRuntimeDto.FromDomain(skill) with
                {
                    // The confirmation shown by the tactical client must quote the exact
                    // sacrifice, including active item/status cost modifiers.
                    EffectiveManaCost = Math.Max(
                        0,
                        (int)Math.Round(
                            skill.ManaCost
                            // EffectiveSkillCostReductionPercent is negative for a beneficial
                            // reduction (e.g. -5 for Mina's "Protection de Him'Lit"), so adding
                            // it (not subtracting) is what shrinks the cost.
                            * (1.0 + combatant.EffectiveSkillCostReductionPercent / 100.0))
                        + combatant.EffectiveFlatManaCostBonus)
                })
                .ToArray());
    }

    private static ResolvedEmotionalAffinityDto ResolveIncomingAffinity(
        CombatantTypeProfile profile,
        EmotionalType incomingRegister)
    {
        var outcome = profile.EffectivenessAgainst(incomingRegister);
        var modifierPercent = profile.MultiplierPercentAgainst(incomingRegister);
        var baseMultiplier = profile.BaseMultiplierAgainst(incomingRegister);
        var effectiveMultiplier = baseMultiplier
            * Math.Max(0.0, 1.0 + modifierPercent / 100.0);

        return new ResolvedEmotionalAffinityDto(
            IncomingRegister: CodeOf(incomingRegister),
            Outcome: outcome.ToString(),
            BaseMultiplier: baseMultiplier,
            ModifierPercent: modifierPercent,
            EffectiveMultiplier: effectiveMultiplier,
            Modifiers: profile.Modifiers
                .Where(modifier => !modifier.IsExpired && modifier.IncomingRegister == incomingRegister)
                .OrderByDescending(modifier => modifier.Priority)
                .ThenBy(modifier => modifier.SourceKey, StringComparer.Ordinal)
                .Select(modifier => new CombatantAffinityModifierDto(
                    modifier.SourceKey,
                    CodeOf(modifier.IncomingRegister),
                    modifier.OutcomeOverride?.ToString(),
                    modifier.MultiplierPercent,
                    modifier.Priority,
                    modifier.RemainingActivations))
                .ToArray());
    }

    private static string CodeOf(EmotionalType register) =>
        register.ToString().ToLowerInvariant();
}

public sealed record ResolvedEmotionalAffinityDto(
    string IncomingRegister,
    string Outcome,
    double BaseMultiplier,
    int ModifierPercent,
    double EffectiveMultiplier,
    IReadOnlyCollection<CombatantAffinityModifierDto> Modifiers);

public sealed record CombatantAffinityModifierDto(
    string SourceKey,
    string IncomingRegister,
    string? OutcomeOverride,
    int MultiplierPercent,
    int Priority,
    int? RemainingActivations);
