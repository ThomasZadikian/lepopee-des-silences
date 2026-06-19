using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Curses;

public interface ICurseDefinition : ICatalogContent
{
    string? NarrativeText { get; }

    int Severity { get; }

    string Duration { get; }

    string? Trigger { get; }

    string? EffectSetKey { get; }
}
