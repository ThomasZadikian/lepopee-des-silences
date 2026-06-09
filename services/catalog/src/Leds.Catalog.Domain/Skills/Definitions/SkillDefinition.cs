using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

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
        int basePower)
        : base(id, key, name, description, version, status)
    {
        SkillType = skillType;
        TargetingType = targetingType;
        EffectType = effectType;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
    }

    public string SkillType { get; }

    public string TargetingType { get; }

    public string EffectType { get; }

    public int ManaCost { get; }

    public int ChargeCost { get; }

    public int BasePower { get; }

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
        CatalogContentStatus status = CatalogContentStatus.Draft)
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
            basePower);
    }
}
