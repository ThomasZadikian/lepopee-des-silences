using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record ActivePalaceLawDto(
    Guid LawId,
    string Key,
    string Name,
    string Version,
    IReadOnlyCollection<string> Domains)
{
    public static ActivePalaceLawDto FromDomain(ActivePalaceLaw activeLaw)
    {
        return new ActivePalaceLawDto(
            activeLaw.LawId.Value,
            activeLaw.Key,
            activeLaw.Name,
            activeLaw.Version,
            activeLaw.Domains.Select(domain => domain.ToString()).ToArray());
    }
}