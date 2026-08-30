using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcDialogueChoiceDto(
    string Key,
    string Label,
    IReadOnlyCollection<DialogueRequirementDto> Requirements,
    IReadOnlyCollection<DialogueConsequenceDto> Consequences,
    string? NextNodeKey)
{
    public static NpcDialogueChoiceDto FromDomain(NpcDialogueChoice c) => new(
        c.Key,
        c.Label,
        c.Requirements.Select(DialogueRequirementDto.FromDomain).ToArray(),
        c.Consequences.Select(DialogueConsequenceDto.FromDomain).ToArray(),
        c.NextNodeKey);
}