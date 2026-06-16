using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacterSkill
{
    private PlayerCharacterSkill(string skillDefinitionKey, DateTimeOffset unlockedAtUtc, string? source)
    {
        SkillDefinitionKey = skillDefinitionKey;
        UnlockedAtUtc = unlockedAtUtc;
        Source = source;
    }

    public string SkillDefinitionKey { get; }
    public DateTimeOffset UnlockedAtUtc { get; }
    public string? Source { get; }

    public static PlayerCharacterSkill Create(
        string skillDefinitionKey,
        DateTimeOffset unlockedAtUtc,
        string? source = null)
    {
        if (string.IsNullOrWhiteSpace(skillDefinitionKey))
            throw new DomainException("Skill definition key is required.");

        return new PlayerCharacterSkill(skillDefinitionKey.Trim(), unlockedAtUtc, string.IsNullOrWhiteSpace(source) ? null : source.Trim());
    }
}
