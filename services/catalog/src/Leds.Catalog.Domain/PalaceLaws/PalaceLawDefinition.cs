using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.PalaceLaws;

public sealed class PalaceLawDefinition : CatalogContentBase, IPalaceLawDefinition
{
    private readonly List<PalaceLawImpactDomain> _impactDomains;

    private PalaceLawDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        PalaceLawVisibility visibility,
        int priority,
        IReadOnlyCollection<PalaceLawImpactDomain> impactDomains)
        : base(id, key, name, description, version, status)
    {
        Visibility = visibility;
        Priority = priority;
        _impactDomains = impactDomains.ToList();
    }

    public PalaceLawVisibility Visibility { get; }

    public int Priority { get; }

    public IReadOnlyCollection<PalaceLawImpactDomain> ImpactDomains =>
        _impactDomains.AsReadOnly();

    public static PalaceLawDefinition Create(
        string key,
        string name,
        string? description,
        string version,
        PalaceLawVisibility visibility,
        int priority,
        IReadOnlyCollection<PalaceLawImpactDomain> impactDomains,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        if (priority < 0)
        {
            throw new DomainException("Palace law priority cannot be negative.");
        }

        if (impactDomains is null || impactDomains.Count == 0)
        {
            throw new DomainException("Palace law must target at least one impact domain.");
        }

        var distinctImpactDomains = impactDomains
            .Distinct()
            .ToArray();

        return new PalaceLawDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            visibility,
            priority,
            distinctImpactDomains);
    }
}