using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record RunPartySnapshotDto(
    IReadOnlyCollection<RunPartyMemberDto> Members)
{
    public static RunPartySnapshotDto? FromDomain(
        RunPlayerSnapshot? snapshot,
        PlayerRuntimeState playerState)
    {
        if (snapshot is null)
            return null;

        var members = snapshot.Characters
            .Select((character, index) =>
            {
                bool isMain = index == 0;
                return new RunPartyMemberDto(
                    Id: character.CharacterId,
                    DefinitionKey: character.DefinitionKey,
                    DisplayName: character.DisplayName,
                    MaxVitality: character.StatBlock.MaxVitality,
                    CurrentVitality: isMain ? playerState.CurrentVitality : character.StatBlock.MaxVitality,
                    Guard: isMain ? playerState.Guard : 0,
                    Mana: isMain ? playerState.Mana : character.StatBlock.Mana,
                    Charge: isMain ? playerState.Charge : character.StatBlock.Charge,
                    IsActive: isMain,
                    IsDefeated: isMain && playerState.IsDefeated,
                    Skills: character.Skills
                        .Select(RunPartyMemberSkillDto.FromDomain)
                        .ToArray());
            })
            .ToArray();

        return new RunPartySnapshotDto(members);
    }
}

public sealed record RunPartyMemberDto(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    int MaxVitality,
    int CurrentVitality,
    int Guard,
    int Mana,
    int Charge,
    bool IsActive,
    bool IsDefeated,
    IReadOnlyCollection<RunPartyMemberSkillDto> Skills);

public sealed record RunPartyMemberSkillDto(
    string Key,
    string DisplayName,
    string SkillType,
    string TargetingMode,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower)
{
    public static RunPartyMemberSkillDto FromDomain(RunCharacterSkillSnapshot skill) => new(
        skill.SkillDefinitionKey,
        skill.DisplayName,
        skill.SkillType,
        skill.TargetingMode,
        skill.EffectType,
        skill.ManaCost,
        skill.ChargeCost,
        skill.BasePower);
}
