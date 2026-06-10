using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record PlayerRuntimeStateDto(
    int MaxVitality,
    int CurrentVitality,
    int Guard,
    int Mana,
    int Charge,
    IReadOnlyCollection<PlayerRuntimeSkillDto> Skills)
{
    public static PlayerRuntimeStateDto FromDomain(PlayerRuntimeState state)
    {
        return new PlayerRuntimeStateDto(
            state.MaxVitality,
            state.CurrentVitality,
            state.Guard,
            state.Mana,
            state.Charge,
            state.Skills.Select(PlayerRuntimeSkillDto.FromDomain).ToArray());
    }
}

public sealed record PlayerRuntimeSkillDto(
    string Key,
    string DisplayName,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower)
{
    public static PlayerRuntimeSkillDto FromDomain(PlayerRuntimeSkill skill)
    {
        return new PlayerRuntimeSkillDto(
            skill.Key,
            skill.DisplayName,
            skill.SkillType,
            skill.TargetingType,
            skill.EffectType,
            skill.ManaCost,
            skill.ChargeCost,
            skill.BasePower);
    }
}
