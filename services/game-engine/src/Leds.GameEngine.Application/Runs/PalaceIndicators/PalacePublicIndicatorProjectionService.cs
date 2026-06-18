using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.PalaceIndicators;

public sealed class PalacePublicIndicatorProjectionService : IPalacePublicIndicatorProjectionService
{
    public IReadOnlyCollection<PalacePublicIndicatorDto> Project(
        Run run,
        IReadOnlyCollection<PalaceIndicator>? persistedIndicators = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        var indicators = new List<PalacePublicIndicatorDto>();

        indicators.AddRange((persistedIndicators ?? [])
            .Where(indicator => !indicator.IsExpired)
            .Select(PalacePublicIndicatorDto.FromDomain));

        indicators.AddRange(ProjectActiveLaws(run));

        if (run.ActiveCurse is { IsConsumed: false } activeCurse)
        {
            indicators.Add(ProjectActiveCurse(activeCurse));
        }

        var activeClimate = RoomDto.FromDomain(run.CurrentRoom, run.RunModifiers).ActiveClimate;
        if (!string.IsNullOrWhiteSpace(activeClimate))
        {
            indicators.Add(ProjectActiveClimate(activeClimate));
        }

        return indicators
            .GroupBy(indicator => $"{indicator.Key}\u001f{indicator.Source ?? string.Empty}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static IEnumerable<PalacePublicIndicatorDto> ProjectActiveLaws(Run run)
    {
        foreach (var activeLaw in run.ActivePalaceLaws.Where(law => !law.IsConsumed))
        {
            yield return new PalacePublicIndicatorDto(
                Key: $"law:{activeLaw.Key}",
                Label: activeLaw.DisplayName,
                Description: string.IsNullOrWhiteSpace(activeLaw.Description) ? null : activeLaw.Description,
                Category: "law",
                Level: null,
                Tone: "mystery",
                Source: "law");
        }
    }

    private static PalacePublicIndicatorDto ProjectActiveCurse(ActiveCurse curse)
    {
        return new PalacePublicIndicatorDto(
            Key: $"curse:{curse.Key}",
            Label: curse.DisplayName,
            Description: string.IsNullOrWhiteSpace(curse.Description) ? null : curse.Description,
            Category: "curse",
            Level: NormalizeCurseSeverity(curse.Severity),
            Tone: "danger",
            Source: "curse");
    }

    private static PalacePublicIndicatorDto ProjectActiveClimate(string climate)
    {
        var normalized = climate.Trim();
        var publicClimate = normalized switch
        {
            "Grey" => (Label: "Climat : Grisaille", Description: "Un voile gris pèse sur la salle.", Tone: "unstable"),
            "Rain" => (Label: "Climat : Pluie", Description: "La pluie transforme le rythme de la salle.", Tone: "calm"),
            "Heatwave" => (Label: "Climat : Canicule", Description: "La chaleur rend la salle plus hostile.", Tone: "danger"),
            "Hail" => (Label: "Climat : Grele", Description: "La grele annonce une salle instable.", Tone: "unstable"),
            _ => (Label: $"Climat : {normalized}", Description: "Le climat de la salle change la traversee.", Tone: "mystery")
        };

        return new PalacePublicIndicatorDto(
            Key: $"climate:{normalized.ToLowerInvariant()}",
            Label: publicClimate.Label,
            Description: publicClimate.Description,
            Category: "climate",
            Level: null,
            Tone: publicClimate.Tone,
            Source: "room");
    }

    private static string NormalizeCurseSeverity(int severity)
    {
        return severity switch
        {
            >= 4 => "high",
            >= 2 => "medium",
            _ => "low"
        };
    }
}
