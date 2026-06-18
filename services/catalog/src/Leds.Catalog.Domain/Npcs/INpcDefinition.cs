using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.Npcs;

public interface INpcDefinition : ICatalogContent
{
    IReadOnlyCollection<string> Tags { get; }

    IReadOnlyCollection<string> CompatibleRoomTypes { get; }

    IReadOnlyCollection<string> CompatiblePalaceRoomStates { get; }

    IReadOnlyCollection<string> CompatibleRoomClimates { get; }

    int? MinDepth { get; }

    int? MaxDepth { get; }
}
