using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Players;

public sealed record PlayerProfileDto(
    Guid Id,
    string DisplayName,
    IReadOnlyCollection<PlayerCharacterDto> Characters,
    PlayerProgressionDto Progression,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static PlayerProfileDto FromDomain(PlayerProfile profile)
    {
        return new PlayerProfileDto(
            profile.Id.Value,
            profile.DisplayName,
            profile.Roster.Characters.Select(PlayerCharacterDto.FromDomain).ToArray(),
            PlayerProgressionDto.FromDomain(profile.Progression),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }
}

public sealed record PlayerCharacterDto(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    int MaxVitality,
    int BaseMana,
    int BaseCharge,
    IReadOnlyCollection<string> SkillKeys,
    IReadOnlyCollection<PlayerCharacterSkillDto> Skills,
    PlayerCharacterStatsDto Stats,
    int MaxEquippedSkills)
{
    public static PlayerCharacterDto FromDomain(PlayerCharacter character)
    {
        return new PlayerCharacterDto(
            character.Id.Value,
            character.DefinitionKey,
            character.DisplayName,
            character.MaxVitality,
            character.BaseMana,
            character.BaseCharge,
            character.SkillKeys.ToArray(),
            character.Skills.Select(PlayerCharacterSkillDto.FromDomain).ToArray(),
            PlayerCharacterStatsDto.FromDomain(character.StatBlock),
            PlayerCharacter.MaxEquippedSkills);
    }
}

public sealed record PlayerCharacterSkillDto(
    string SkillKey,
    DateTimeOffset UnlockedAtUtc,
    string? Source,
    bool IsEquipped)
{
    public static PlayerCharacterSkillDto FromDomain(PlayerCharacterSkill skill)
    {
        return new PlayerCharacterSkillDto(
            skill.SkillDefinitionKey,
            skill.UnlockedAtUtc,
            skill.Source,
            skill.IsEquipped);
    }
}

public sealed record PlayerCharacterStatsDto(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge)
{
    public static PlayerCharacterStatsDto FromDomain(PlayerCharacterStatBlock statBlock)
    {
        return new PlayerCharacterStatsDto(
            statBlock.MaxVitality,
            statBlock.AttackPower,
            statBlock.Defense,
            statBlock.StartingGuard,
            statBlock.Speed,
            statBlock.Initiative,
            statBlock.Recovery,
            statBlock.Focus,
            statBlock.Mana,
            statBlock.Charge);
    }
}

public sealed record PlayerProgressionDto(
    int TotalRunsStarted,
    int TotalRunsCompleted,
    int TotalRunsFailed,
    int TotalRunsAbandoned,
    int UnspentStatPoints,
    int TotalStatPointsEarned)
{
    public static PlayerProgressionDto FromDomain(PlayerProgression progression)
    {
        return new PlayerProgressionDto(
            progression.TotalRunsStarted,
            progression.TotalRunsCompleted,
            progression.TotalRunsFailed,
            progression.TotalRunsAbandoned,
            progression.UnspentStatPoints,
            progression.TotalStatPointsEarned);
    }
}
