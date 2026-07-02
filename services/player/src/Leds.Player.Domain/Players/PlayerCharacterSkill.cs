using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerCharacterSkill
{
    private PlayerCharacterSkill(string skillDefinitionKey, DateTimeOffset unlockedAtUtc, string? source, bool isEquipped)
    {
        SkillDefinitionKey = skillDefinitionKey;
        UnlockedAtUtc = unlockedAtUtc;
        Source = source;
        IsEquipped = isEquipped;
    }

    public string SkillDefinitionKey { get; }
    public DateTimeOffset UnlockedAtUtc { get; }
    public string? Source { get; }
    public bool IsEquipped { get; private set; }

    public static PlayerCharacterSkill Create(
        string skillDefinitionKey,
        DateTimeOffset unlockedAtUtc,
        string? source = null,
        bool isEquipped = false)
    {
        if (string.IsNullOrWhiteSpace(skillDefinitionKey))
            throw new DomainException("Skill definition key is required.");

        return new PlayerCharacterSkill(skillDefinitionKey.Trim(), unlockedAtUtc, string.IsNullOrWhiteSpace(source) ? null : source.Trim(), isEquipped);
    }

    internal void Equip() => IsEquipped = true;

    internal void Unequip() => IsEquipped = false;
}
