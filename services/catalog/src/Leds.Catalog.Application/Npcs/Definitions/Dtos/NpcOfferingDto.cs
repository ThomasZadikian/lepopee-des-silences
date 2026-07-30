using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcOfferingDto(
    string Key,
    string Kind,
    string? TargetKey,
    int Amount,
    bool IsMajor,
    IReadOnlyCollection<DialogueRequirementDto> UnlockConditions,
    CompanionKitSpecDto? CompanionKit = null)
{
    public static NpcOfferingDto FromDomain(NpcOffering offering) => new(
        offering.Key,
        offering.Kind.ToString(),
        offering.TargetKey,
        offering.Amount,
        offering.IsMajor,
        offering.UnlockConditions.Select(DialogueRequirementDto.FromDomain).ToArray(),
        offering.CompanionKit is null ? null : CompanionKitSpecDto.FromDomain(offering.CompanionKit));
}

public sealed record CompanionKitSpecDto(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyCollection<string> SkillKeys,
    int MagicAttack = 0,
    int MagicDefense = 0)
{
    public static CompanionKitSpecDto FromDomain(CompanionKitSpec kit) => new(
        kit.MaxVitality,
        kit.AttackPower,
        kit.Defense,
        kit.StartingGuard,
        kit.Speed,
        kit.Initiative,
        kit.Focus,
        kit.Mana,
        kit.Charge,
        kit.SkillKeys.ToArray(),
        kit.MagicAttack,
        kit.MagicDefense);
}
