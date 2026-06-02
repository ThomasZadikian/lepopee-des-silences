using Leds.Catalog.Domain.Items;

namespace Leds.Catalog.Application.Items.Ports;

public interface IItemTemplateReadStore
{
    Task<IReadOnlyCollection<IItemTemplate>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IItemTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}