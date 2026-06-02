using Leds.Catalog.Domain.EventTemplates;

namespace Leds.Catalog.Application.EventTemplates.Dtos;

public sealed record EventTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string Type,
    string DefaultOutcomeKind,
    int MinRiskLevel,
    int MaxRiskLevel,
    bool RequiresPlayerChoice,
    IReadOnlyCollection<string> NarrativeTags)
{
    public static EventTemplateDto FromDomain(
        IEventTemplate eventTemplate)
    {
        return new EventTemplateDto(
            eventTemplate.Id.Value,
            eventTemplate.Key.Value,
            eventTemplate.Name.Value,
            eventTemplate.Description.Value,
            eventTemplate.Version.Value,
            eventTemplate.Status.ToString(),
            eventTemplate.Type.ToString(),
            eventTemplate.DefaultOutcomeKind.ToString(),
            eventTemplate.MinRiskLevel,
            eventTemplate.MaxRiskLevel,
            eventTemplate.RequiresPlayerChoice,
            eventTemplate.NarrativeTags.ToArray());
    }
}