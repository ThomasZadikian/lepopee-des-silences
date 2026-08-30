using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.Application.Npcs.Definitions.Ports;

public interface INpcDefinitionReadStore
{
    Task<IReadOnlyCollection<INpcDefinition>> ListActiveAsync(
        CancellationToken cancellationToken);
}
