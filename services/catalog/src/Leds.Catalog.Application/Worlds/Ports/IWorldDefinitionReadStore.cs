using Leds.Catalog.Application.Worlds.Dtos;

namespace Leds.Catalog.Application.Worlds.Ports;

public interface IWorldDefinitionReadStore
{
    Task<IReadOnlyCollection<WorldDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken);
}
