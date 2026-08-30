using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Dtos;

public sealed record NpcPersonaDto(
    string Tone,
    string Register,
    IReadOnlyCollection<string> Needs,
    IReadOnlyCollection<string> Offerings)
{
    public static NpcPersonaDto FromDomain(NpcPersona persona) => new(
        persona.Tone,
        persona.Register.ToString(),
        persona.Needs.ToArray(),
        persona.Offerings.ToArray());
}