using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcDialogueGraphDto(
    string Key,
    string Version,
    string EntryNodeKey,
    IReadOnlyDictionary<string, NpcDialogueNodeDto> Nodes)
{
    public static NpcDialogueGraphDto FromDomain(NpcDialogueGraph g) => new(
        g.Key,
        g.Version,
        g.EntryNodeKey,
        g.Nodes.ToDictionary(kv => kv.Key, kv => NpcDialogueNodeDto.FromDomain(kv.Value)));
}