using Leds.Catalog.Application.EffectSets.Ports;
using MediatR;

namespace Leds.Catalog.Application.EffectSets.GetEffectSetByKey;

public sealed class GetEffectSetByKeyQueryHandler
    : IRequestHandler<GetEffectSetByKeyQuery, GetEffectSetByKeyResponse>
{
    private readonly IEffectSetReadStore _readStore;

    public GetEffectSetByKeyQueryHandler(IEffectSetReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetEffectSetByKeyResponse> Handle(
        GetEffectSetByKeyQuery request,
        CancellationToken cancellationToken)
    {
        return new GetEffectSetByKeyResponse(
            await _readStore.GetDtoByKeyAsync(request.Key, cancellationToken));
    }
}
