using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Combat;
using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Skills;

public sealed class SkillTemplate : CatalogContentBase, ISkillTemplate
{
    private SkillTemplate(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        CombatElement element,
        SkillEffectType effectType,
        SkillTargetType targetType,
        int manaCost,
        int chargeCost,
        int basePower,
        int healPower)
        : base(id, key, name, description, version, status)
    {
        Element = element;
        EffectType = effectType;
        TargetType = targetType;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
        HealPower = healPower;
    }

    public CombatElement Element { get; }

    public SkillEffectType EffectType { get; }

    public SkillTargetType TargetType { get; }

    public int ManaCost { get; }

    public int ChargeCost { get; }

    public int BasePower { get; }

    public int HealPower { get; }

    public static SkillTemplate Create(
        string key,
        string name,
        string? description,
        string version,
        CombatElement element,
        SkillEffectType effectType,
        SkillTargetType targetType,
        int manaCost,
        int chargeCost,
        int basePower,
        int healPower,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (manaCost < 0)
        {
            throw new DomainException("Skill mana cost cannot be negative.");
        }

        if (chargeCost < 0)
        {
            throw new DomainException("Skill charge cost cannot be negative.");
        }

        if (basePower < 0)
        {
            throw new DomainException("Skill base power cannot be negative.");
        }

        if (healPower < 0)
        {
            throw new DomainException("Skill heal power cannot be negative.");
        }

        if (effectType == SkillEffectType.Damage && basePower == 0)
        {
            throw new DomainException("Damage skill must have a base power greater than 0.");
        }

        if (effectType == SkillEffectType.Heal && healPower == 0)
        {
            throw new DomainException("Heal skill must have a heal power greater than 0.");
        }

        return new SkillTemplate(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            element,
            effectType,
            targetType,
            manaCost,
            chargeCost,
            basePower,
            healPower);
    }
}