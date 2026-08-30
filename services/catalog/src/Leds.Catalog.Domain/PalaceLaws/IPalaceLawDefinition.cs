using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.PalaceLaws;

public interface IPalaceLawDefinition : ICatalogContent
{
    PalaceLawVisibility Visibility { get; }

    int Priority { get; }

    IReadOnlyCollection<PalaceLawImpactDomain> ImpactDomains { get; }
}