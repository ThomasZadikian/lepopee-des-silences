using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.Runs;

public sealed class RunCharacterSkillSnapshot
{
    private RunCharacterSkillSnapshot(
        Guid id,
        string skillDefinitionKey,
        string displayName,
        string skillType,
        string targetingMode,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower)
    {
        Id = id;
        SkillDefinitionKey = skillDefinitionKey;
        DisplayName = displayName;
        SkillType = skillType;
        TargetingMode = targetingMode;
        EffectType = effectType;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
    }

    public Guid Id { get; }
    public string SkillDefinitionKey { get; }
    public string DisplayName { get; }
    public string SkillType { get; }
    public string TargetingMode { get; }
    public string EffectType { get; }
    public int ManaCost { get; }
    public int ChargeCost { get; }
    public int BasePower { get; }

    public static RunCharacterSkillSnapshot Create(
        string skillDefinitionKey,
        string displayName,
        string skillType,
        string targetingMode,
        string effectType,
        int manaCost = 0,
        int chargeCost = 0,
        int basePower = 0)
    {
        if (string.IsNullOrWhiteSpace(skillDefinitionKey))
            throw new DomainException("Skill definition key is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Skill display name is required.");

        return new RunCharacterSkillSnapshot(
            Guid.NewGuid(),
            skillDefinitionKey.Trim(),
            displayName.Trim(),
            skillType,
            targetingMode,
            effectType,
            manaCost,
            chargeCost,
            basePower);
    }

    public static RunCharacterSkillSnapshot Rehydrate(
        Guid id,
        string skillDefinitionKey,
        string displayName,
        string skillType,
        string targetingMode,
        string effectType,
        int manaCost = 0,
        int chargeCost = 0,
        int basePower = 0)
    {
        return new RunCharacterSkillSnapshot(
            id,
            skillDefinitionKey,
            displayName,
            skillType,
            targetingMode,
            effectType,
            manaCost,
            chargeCost,
            basePower);
    }
}
