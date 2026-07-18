using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record ActivePalaceLawDto(
    Guid LawId,
    string Key,
    string Name,
    string DisplayName,
    string Description,
    string Version,
    string Rarity,
    string Polarity,
    IReadOnlyCollection<string> Domains)
{
    public static ActivePalaceLawDto FromDomain(ActivePalaceLaw activeLaw)
    {
        return new ActivePalaceLawDto(
            activeLaw.LawId.Value,
            activeLaw.Key,
            activeLaw.Name,
            activeLaw.DisplayName,
            activeLaw.Description,
            activeLaw.Version,
            activeLaw.Rarity,
            activeLaw.Polarity,
            activeLaw.Domains.Select(domain => domain.ToString()).ToArray());
    }
}