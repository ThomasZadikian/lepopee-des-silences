using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.Domain.Abstractions;

public interface ICatalogContent
{
    CatalogContentId Id { get; }

    CatalogContentKey Key { get; }

    CatalogContentName Name { get; }

    CatalogContentDescription Description { get; }

    CatalogContentVersion Version { get; }

    CatalogContentStatus Status { get; }

    bool IsActive { get; }

    bool IsDraft { get; }

    bool IsDeprecated { get; }

    bool IsDisabled { get; }
}