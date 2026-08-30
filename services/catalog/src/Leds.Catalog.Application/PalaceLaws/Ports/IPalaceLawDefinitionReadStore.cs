using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Domain.PalaceLaws;

namespace Leds.Catalog.Application.PalaceLaws.Ports;

public interface IPalaceLawDefinitionReadStore
{
    Task<IReadOnlyCollection<IPalaceLawDefinition>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IPalaceLawDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}

public interface IPalaceLawDefinitionDtoReadStore
{
    Task<IReadOnlyCollection<PalaceLawDefinitionDto>> ListActiveDtosAsync(
        CancellationToken cancellationToken);

    Task<PalaceLawDefinitionDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}
