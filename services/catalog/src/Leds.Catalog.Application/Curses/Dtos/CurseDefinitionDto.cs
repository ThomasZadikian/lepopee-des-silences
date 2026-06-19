using Leds.Catalog.Domain.Curses;

namespace Leds.Catalog.Application.Curses.Dtos;

public sealed record CurseDefinitionDto(
    Guid Id,
    string Key,
    string Version,
    string DisplayName,
    string Description,
    string? NarrativeText,
    int Severity,
    string Duration,
    string? Trigger,
    string? EffectSetKey,
    string Status)
{
    public static CurseDefinitionDto FromDomain(ICurseDefinition definition)
    {
        return new CurseDefinitionDto(
            definition.Id.Value,
            definition.Key.Value,
            definition.Version.Value,
            definition.Name.Value,
            definition.Description.Value,
            definition.NarrativeText,
            definition.Severity,
            definition.Duration,
            definition.Trigger,
            definition.EffectSetKey,
            definition.Status.ToString());
    }
}
