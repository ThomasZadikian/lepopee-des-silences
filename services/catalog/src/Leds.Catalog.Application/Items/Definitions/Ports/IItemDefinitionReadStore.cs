using Leds.Catalog.Application.Items.Definitions.Dtos;

namespace Leds.Catalog.Application.Items.Definitions.Ports;

public interface IItemDefinitionReadStore
{
    Task<ItemDefinitionDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ItemDefinitionDto>> ListActiveDtosAsync(
        CancellationToken cancellationToken);
}
