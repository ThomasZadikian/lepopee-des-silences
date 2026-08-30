using Leds.Catalog.Application.NpcReputationAffinities.Dtos;

namespace Leds.Catalog.Application.NpcReputationAffinities.Ports;

public interface INpcReputationAffinityReadStore
{
    Task<IReadOnlyCollection<NpcReputationAffinityDto>> ListAllAsync(CancellationToken cancellationToken);
}
