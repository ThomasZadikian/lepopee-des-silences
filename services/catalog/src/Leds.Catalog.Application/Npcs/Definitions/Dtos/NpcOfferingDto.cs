using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcOfferingDto(
    string Key,
    string Kind,
    string? TargetKey,
    int Amount,
    bool IsMajor,
    IReadOnlyCollection<DialogueRequirementDto> UnlockConditions)
{
    public static NpcOfferingDto FromDomain(NpcOffering offering) => new(
        offering.Key,
        offering.Kind.ToString(),
        offering.TargetKey,
        offering.Amount,
        offering.IsMajor,
        offering.UnlockConditions.Select(DialogueRequirementDto.FromDomain).ToArray());
}
