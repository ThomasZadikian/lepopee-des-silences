using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatantRuntimeDto(
    Guid Id,
    string SourceKey,
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
    string AttackType,
    IReadOnlyCollection<string> WeakTo,
    IReadOnlyCollection<string> ResistantTo,
    IReadOnlyCollection<string> ImmuneTo,
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

    public static CombatantRuntimeDto FromDomain(Combatant combatant, int currentTick)
    {
        var profile = TypeProvider.Resolve(combatant, EmotionalAffinityMatrixSnapshot.Canonical);

        return new CombatantRuntimeDto(
            Id: combatant.Id.Value,
            SourceKey: combatant.SourceKey,
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
            AttackType: profile.AttackType.ToString(),
            WeakTo: profile.WeakTo.Select(t => t.ToString()).ToArray(),
            ResistantTo: profile.ResistantTo.Select(t => t.ToString()).ToArray(),
            ImmuneTo: profile.ImmuneTo.Select(t => t.ToString()).ToArray(),
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
}
