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
    IReadOnlyCollection<string> SkillKeys)
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
            character.SkillKeys.ToArray());
    }
}

public sealed record PlayerProgressionDto(
    int TotalRunsStarted,
    int TotalRunsCompleted,
    int TotalRunsFailed,
    int TotalRunsAbandoned)
{
    public static PlayerProgressionDto FromDomain(PlayerProgression progression)
    {
        return new PlayerProgressionDto(
            progression.TotalRunsStarted,
            progression.TotalRunsCompleted,
            progression.TotalRunsFailed,
            progression.TotalRunsAbandoned);
    }
}
