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
    IReadOnlyCollection<CombatantSkillRuntimeDto> Skills)
{
    public static CombatantRuntimeDto FromDomain(Combatant combatant)
    {
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
            Skills: combatant.Skills
                .Select(CombatantSkillRuntimeDto.FromDomain)
                .ToArray());
    }
}
