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
    int Charge,
    string Status,
    string AttackType,
    IReadOnlyCollection<string> WeakTo,
    IReadOnlyCollection<string> ResistantTo,
    IReadOnlyCollection<string> ImmuneTo,
    int AttackPower,
    int Defense,
    int Speed,
    int Focus,
    int AtbGauge,
    int AtbFillPerTick,
    double ThreatValue,
    IReadOnlyCollection<CombatantStatusEffectDto> StatusEffects,
    IReadOnlyCollection<CombatantSkillRuntimeDto> Skills)
{
    // Stateless pure provider; safe to share. Resolves the emotional type and
    // affinities (honours an item-driven AttackTypeOverride on the combatant).
    private static readonly EmotionalTypeProfileProvider TypeProvider = new();

    public static CombatantRuntimeDto FromDomain(Combatant combatant)
    {
        var profile = TypeProvider.Resolve(combatant);

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
            AtbGauge: combatant.AtbGauge,
            AtbFillPerTick: combatant.AtbFillPerTick,
            ThreatValue: combatant.ThreatValue,
            StatusEffects: combatant.StatusEffects
                .Select(e => new CombatantStatusEffectDto(
                    e.Key,
                    e.DisplayName,
                    e.Kind.ToString(),
                    e.Stat.ToString(),
                    e.Magnitude,
                    e.Stacks))
                .ToArray(),
            Skills: combatant.Skills
                .Select(CombatantSkillRuntimeDto.FromDomain)
                .ToArray());
    }
}