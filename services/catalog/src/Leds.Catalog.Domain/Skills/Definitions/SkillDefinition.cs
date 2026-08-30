using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;
using Leds.Catalog.Domain.Gameplay;
using Leds.Catalog.Domain.Npcs;
using Leds.Catalog.Domain.Skills;

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
        string category,
        int manaCost,
        int chargeCost,
        int basePower,
        IReadOnlyList<SkillEffectSpec> effects,
        bool basePowerIsPercentOfMaxVitality,
        int tacticalRange,
        string tacticalAreaShape,
        bool requiresLineOfSight,
        int cooldown,
        bool isUltimate,
        string emotionalRegister,
        string audience,
        IReadOnlyList<string> allowedArchetypes)
        : base(id, key, name, description, version, status)
    {
        SkillType = skillType;
        TargetingType = targetingType;
        EffectType = effectType;
        Category = category;
        ManaCost = manaCost;
        ChargeCost = chargeCost;
        BasePower = basePower;
        Effects = effects;
        BasePowerIsPercentOfMaxVitality = basePowerIsPercentOfMaxVitality;
        TacticalRange = tacticalRange;
        TacticalAreaShape = tacticalAreaShape;
        RequiresLineOfSight = requiresLineOfSight;
        Cooldown = cooldown;
        IsUltimate = isUltimate;
        EmotionalRegister = emotionalRegister;
        Audience = audience;
        AllowedArchetypes = allowedArchetypes;
    }

    public string SkillType { get; }

    public string TargetingType { get; }

    public string EffectType { get; }

    public string Category { get; }

    public int ManaCost { get; }

    public int ChargeCost { get; }

    public int BasePower { get; }
    public int TacticalRange { get; }
    public string TacticalAreaShape { get; }
    public bool RequiresLineOfSight { get; }
    public int Cooldown { get; }
    public bool IsUltimate { get; }
    public string EmotionalRegister { get; }
    public string Audience { get; }
    public IReadOnlyList<string> AllowedArchetypes { get; }
    public IReadOnlyList<SkillEffectSpec> Effects { get; }
    /// <summary>When true, BasePower is a percentage of the target's MaxVitality (instant
    /// heal), not a flat amount — e.g. Mané's "Favorite de Elise" (+15% PV instantly).</summary>
    public bool BasePowerIsPercentOfMaxVitality { get; }

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
        IReadOnlyList<SkillEffectSpec>? effects = null,
        string category = "Physical",
        bool basePowerIsPercentOfMaxVitality = false,
        int tacticalRange = 1,
        string tacticalAreaShape = "Single",
        bool requiresLineOfSight = false,
        int cooldown = 0,
        bool isUltimate = false,
        string? emotionalRegister = null,
        string audience = "Player",
        IReadOnlyList<string>? allowedArchetypes = null)
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

        if (tacticalRange < 0)
        {
            throw new DomainException("Skill definition tactical range cannot be negative.");
        }

        var normalizedShape = tacticalAreaShape?.Trim();
        if (normalizedShape is not ("Single" or "Cross" or "Diamond" or "Map"))
        {
            throw new DomainException("Skill definition tactical area shape must be Single, Cross, Diamond or Map.");
        }

        if (cooldown < 0)
        {
            throw new DomainException("Skill definition cooldown cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(emotionalRegister))
        {
            throw new DomainException("Skill definition emotional register is required.");
        }

        var parsedRegister = EmotionalRegisterCatalog.Parse(emotionalRegister);
        SkillEffectSpecValidator.Validate(key, effects ?? []);

        var normalizedAudience = string.IsNullOrWhiteSpace(audience) ? "Player" : audience.Trim();
        if (normalizedAudience is not ("Player" or "Enemy" or "Any"))
        {
            throw new DomainException("Skill definition audience must be Player, Enemy or Any.");
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
            string.IsNullOrWhiteSpace(category) ? "Physical" : category.Trim(),
            manaCost,
            chargeCost,
            basePower,
            effects ?? [],
            basePowerIsPercentOfMaxVitality,
            tacticalRange,
            normalizedShape,
            requiresLineOfSight,
            cooldown,
            isUltimate,
            EmotionalRegisterCatalog.CodeOf(parsedRegister),
            normalizedAudience,
            allowedArchetypes ?? []);
    }
}
