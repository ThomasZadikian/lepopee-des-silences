using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcWoundDto(
    string Key,
    string WoundRegister,
    string Reversibility,
    int TenseThreshold,
    int RuptureThreshold,
    IReadOnlyCollection<NpcTransgressionDto> Transgressions,
    string? RupturedNarrativeKey)
{
    public static NpcWoundDto FromDomain(NpcWound w) => new(
        w.Key,
        w.WoundRegister.ToString(),
        w.Reversibility.ToString(),
        w.TenseThreshold,
        w.RuptureThreshold,
        w.Transgressions.Select(NpcTransgressionDto.FromDomain).ToArray(),
        w.RupturedNarrativeKey);
}