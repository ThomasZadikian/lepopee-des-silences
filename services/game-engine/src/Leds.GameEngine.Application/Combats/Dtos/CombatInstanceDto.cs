using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Dtos;

public sealed record CombatInstanceDto(
    Guid Id,
    string State,
    int Round,
    IReadOnlyCollection<CombatantDto> Combatants,
    Guid? CurrentActorId)
{
    public static CombatInstanceDto FromDomain(CombatInstance combat)
    {
        return new CombatInstanceDto(
            combat.Id.Value,
            combat.State.ToString(),
            combat.Round,
            combat.Combatants.Select(CombatantDto.FromDomain).ToArray(),
            combat.State == CombatState.InProgress
                ? combat.CurrentActorId.Value
                : null);
    }

    public static CombatInstanceDto FromDomain(Combat combat)
    {
        return new CombatInstanceDto(
            combat.Id.Value,
            combat.Status == CombatStatus.Active ? CombatState.InProgress.ToString() : CombatState.Completed.ToString(),
            combat.TurnNumber,
            combat.Allies.Concat(combat.Enemies).Select(CombatantDto.FromDomain).ToArray(),
            combat.ActiveCombatantId?.Value);
    }
}

public sealed record CombatantDto(
    Guid Id,
    string TemplateKey,
    string DisplayName,
    string Side,
    int MaxHealth,
    int CurrentHealth,
    int Attack,
    int Defense,
    int Speed,
    bool IsDefeated)
{
    public static CombatantDto FromDomain(CombatantSnapshot combatant)
    {
        return new CombatantDto(
            combatant.Id.Value,
            combatant.TemplateKey,
            combatant.DisplayName,
            combatant.Side.ToString(),
            combatant.MaxHealth,
            combatant.CurrentHealth,
            combatant.Attack,
            combatant.Defense,
            combatant.Speed,
            combatant.IsDefeated);
    }

    public static CombatantDto FromDomain(Combatant combatant)
    {
        return new CombatantDto(
            combatant.Id.Value,
            combatant.SourceKey,
            combatant.DisplayName,
            combatant.Side.ToString(),
            combatant.MaxVitality,
            combatant.CurrentVitality,
            combatant.BaseStatSnapshot.AttackPower,
            combatant.BaseStatSnapshot.Defense,
            combatant.BaseStatSnapshot.Speed,
            combatant.IsDefeated);
    }
}
