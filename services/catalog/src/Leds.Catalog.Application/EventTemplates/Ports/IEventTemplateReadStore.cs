using Leds.Catalog.Domain.EventTemplates;

namespace Leds.Catalog.Application.EventTemplates.Ports;

public interface IEventTemplateReadStore
{
    Task<IReadOnlyCollection<IEventTemplate>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<IEventTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}