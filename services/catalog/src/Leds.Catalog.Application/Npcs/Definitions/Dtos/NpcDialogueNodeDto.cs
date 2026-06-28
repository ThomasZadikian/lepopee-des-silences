using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcDialogueNodeDto(
    string Key,
    string Speaker,
    IReadOnlyCollection<string> Lines,
    IReadOnlyCollection<NpcDialogueChoiceDto> Choices,
    IReadOnlyCollection<string>? TenseLines,
    IReadOnlyCollection<string>? RupturedLines)
{
    public static NpcDialogueNodeDto FromDomain(NpcDialogueNode n) => new(
        n.Key,
        n.Speaker,
        n.Lines.ToArray(),
        n.Choices.Select(NpcDialogueChoiceDto.FromDomain).ToArray(),
        n.TenseLines?.ToArray(),
        n.RupturedLines?.ToArray());
}