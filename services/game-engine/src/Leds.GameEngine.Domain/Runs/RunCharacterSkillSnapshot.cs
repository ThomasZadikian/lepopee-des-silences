using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Combats.Typing;

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
        int basePower,
        string category,
        bool basePowerIsPercentOfMaxVitality,
        int tacticalRange,
        string tacticalAreaShape,
        bool requiresLineOfSight,
        int cooldown,
        bool isUltimate,
        string emotionalRegister,
        string temporarySlot)
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
        Category = category;
        BasePowerIsPercentOfMaxVitality = basePowerIsPercentOfMaxVitality;
        TacticalRange = tacticalRange;
        TacticalAreaShape = tacticalAreaShape;
        RequiresLineOfSight = requiresLineOfSight;
        Cooldown = cooldown;
        IsUltimate = isUltimate;
        EmotionalRegister = EmotionalTypeCode.ParseRequired(
            emotionalRegister,
            $"Run character skill '{skillDefinitionKey}' emotional register").ToString();
        TemporarySlot = temporarySlot;
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
    public string Category { get; }
    public bool BasePowerIsPercentOfMaxVitality { get; }
    public int TacticalRange { get; }
    public string TacticalAreaShape { get; }
    public bool RequiresLineOfSight { get; }
    public int Cooldown { get; }
    public bool IsUltimate { get; }
    public string EmotionalRegister { get; }
    public string TemporarySlot { get; }

    public static RunCharacterSkillSnapshot Create(
        string skillDefinitionKey,
        string displayName,
        string skillType,
        string targetingMode,
        string effectType,
        int manaCost = 0,
        int chargeCost = 0,
        int basePower = 0,
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = null!,
        string temporarySlot = "Permanent")
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
            basePower,
            category,
            basePowerIsPercentOfMaxVitality,
            tacticalRange,
            tacticalAreaShape,
            requiresLineOfSight,
            cooldown,
            isUltimate,
            emotionalRegister,
            temporarySlot);
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
        int basePower = 0,
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string emotionalRegister = null!,
        string temporarySlot = "Permanent")
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
            basePower,
            category,
            basePowerIsPercentOfMaxVitality,
            tacticalRange,
            tacticalAreaShape,
            requiresLineOfSight,
            cooldown,
            isUltimate,
            emotionalRegister,
            temporarySlot);
    }
}
