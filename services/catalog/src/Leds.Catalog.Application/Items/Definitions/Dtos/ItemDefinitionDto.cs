using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.Application.Items.Definitions.Dtos;

public sealed record ItemDefinitionDto(
    Guid Id,
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    string Category,
    string ItemType,
    string Rarity,
    string UsageMode,
    string Lifecycle,
    string StackPolicy,
    int MaxStack,
    bool IsUsableInCombat,
    bool IsUsableOutsideCombat,
    string? EffectSetKey,
    string Status,
    bool IsPermanentEligible,
    IReadOnlyCollection<ItemEquipmentEffectDto> EquipmentEffects,
    bool IsContainer = false,
    int? ContainerCapacity = null,
    bool IsLiquid = false,
    int EffectValue = 0,
    string? EffectRunType = null);

public sealed record ItemEquipmentEffectDto(
    string Kind,
    string? StatKind,
    int? Amount,
    string? SkillKey,
    string? AffinityRegister)
{
    public static ItemEquipmentEffectDto FromDomain(ItemEquipmentEffect effect) => new(
        effect.Kind.ToString(), effect.StatKind, effect.Amount, effect.SkillKey, effect.AffinityRegister?.ToString());
}
