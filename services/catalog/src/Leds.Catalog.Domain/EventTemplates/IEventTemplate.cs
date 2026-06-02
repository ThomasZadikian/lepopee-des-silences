using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.EventTemplates;

public interface IEventTemplate : ICatalogContent
{
    EventTemplateType Type { get; }

    EventOutcomeKind DefaultOutcomeKind { get; }

    int MinRiskLevel { get; }

    int MaxRiskLevel { get; }

    bool RequiresPlayerChoice { get; }

    IReadOnlyCollection<string> NarrativeTags { get; }
}