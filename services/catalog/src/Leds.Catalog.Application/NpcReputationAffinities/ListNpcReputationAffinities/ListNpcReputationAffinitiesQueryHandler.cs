using Leds.Catalog.Application.NpcReputationAffinities.Ports;
using MediatR;

namespace Leds.Catalog.Application.NpcReputationAffinities.ListNpcReputationAffinities;

public sealed class ListNpcReputationAffinitiesQueryHandler
    : IRequestHandler<ListNpcReputationAffinitiesQuery, ListNpcReputationAffinitiesResponse>
{
    private readonly INpcReputationAffinityReadStore _readStore;
    public ListNpcReputationAffinitiesQueryHandler(INpcReputationAffinityReadStore readStore) => _readStore = readStore;

    public async Task<ListNpcReputationAffinitiesResponse> Handle(
        ListNpcReputationAffinitiesQuery request, CancellationToken cancellationToken)
        => new(await _readStore.ListAllAsync(cancellationToken));
}
