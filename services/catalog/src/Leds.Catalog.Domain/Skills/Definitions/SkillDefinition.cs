using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;

namespace Leds.Catalog.Domain.Skills.Definitions;

public sealed class SkillDefinition : CatalogContentBase, ISkillDefinition
{
    private SkillDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        string? effectKind,
        string? effectStatusKey,
        int effectMagnitude,
        int effectDurationTicks,
        int effectTickInterval,
        string? effectStat)
        : base(id, key, name, description, version, status)
    {
        SkillType = skillType;
        TargetingType = targetingType;
        EffectType = effectType;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
        EffectKind = effectKind;
        EffectStatusKey = effectStatusKey;
        EffectMagnitude = effectMagnitude;
        EffectDurationTicks = effectDurationTicks;
        EffectTickInterval = effectTickInterval;
        EffectStat = effectStat;
    }

    public string SkillType { get; }

    public string TargetingType { get; }

    public string EffectType { get; }

    public int ManaCost { get; }

    public int ChargeCost { get; }

    public int BasePower { get; }
    public string? EffectKind { get; }
    public string? EffectStatusKey { get; }
    public int EffectMagnitude { get; }
    public int EffectDurationTicks { get; }
    public int EffectTickInterval { get; }
    public string? EffectStat { get; }

    public static SkillDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        string skillType,
        string targetingType,
        string effectType,
        int manaCost,
        int chargeCost,
        int basePower,
        CatalogContentStatus status = CatalogContentStatus.Draft,
        string? effectKind = null,
        string? effectStatusKey = null,
        int effectMagnitude = 0,
        int effectDurationTicks = 0,
        int effectTickInterval = 0,
        string? effectStat = null)
    {
        if (string.IsNullOrWhiteSpace(skillType))
        {
            throw new DomainException("Skill definition skill type is required.");
        }

        if (string.IsNullOrWhiteSpace(targetingType))
        {
            throw new DomainException("Skill definition targeting type is required.");
        }

        if (string.IsNullOrWhiteSpace(effectType))
        {
            throw new DomainException("Skill definition effect type is required.");
        }

        if (manaCost < 0)
        {
            throw new DomainException("Skill definition mana cost cannot be negative.");
        }

        if (chargeCost < 0)
        {
            throw new DomainException("Skill definition charge cost cannot be negative.");
        }

        if (basePower < 0)
        {
            throw new DomainException("Skill definition base power cannot be negative.");
        }

        var desc = CatalogContentDescription.From(description);

        if (desc.IsEmpty)
        {
            throw new DomainException("Skill definition description is required.");
        }

        return new SkillDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            desc,
            CatalogContentVersion.From(version),
            status,
            skillType.Trim(),
            targetingType.Trim(),
            effectType.Trim(),
            manaCost,
            chargeCost,
            basePower,
            string.IsNullOrWhiteSpace(effectKind) ? null : effectKind.Trim(),
            string.IsNullOrWhiteSpace(effectStatusKey) ? null : effectStatusKey.Trim(),
            effectMagnitude,
            effectDurationTicks,
            effectTickInterval,
            string.IsNullOrWhiteSpace(effectStat) ? null : effectStat.Trim());
    }
}
