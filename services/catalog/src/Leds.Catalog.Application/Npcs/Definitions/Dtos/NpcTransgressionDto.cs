using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcTransgressionDto(
    string WoundKey,
    string TriggerFlag,
    int RelationshipPenalty)
{
    public static NpcTransgressionDto FromDomain(NpcTransgression t) => new(
        t.WoundKey,
        t.TriggerFlag,
        t.RelationshipPenalty);
}