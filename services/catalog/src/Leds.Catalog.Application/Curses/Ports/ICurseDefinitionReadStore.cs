using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Domain.Curses;

namespace Leds.Catalog.Application.Curses.Ports;

public interface ICurseDefinitionReadStore
{
    Task<IReadOnlyCollection<ICurseDefinition>> ListAvailableAsync(
        CancellationToken cancellationToken);

    Task<ICurseDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}

public interface ICurseDefinitionDtoReadStore
{
    Task<IReadOnlyCollection<CurseDefinitionDto>> ListAvailableDtosAsync(
        CancellationToken cancellationToken);

    Task<CurseDefinitionDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}
