using Leds.Catalog.Application.Items.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;

public sealed class GetItemDefinitionByKeyQueryHandler
    : IRequestHandler<GetItemDefinitionByKeyQuery, GetItemDefinitionByKeyResponse>
{
    private readonly IItemDefinitionReadStore _readStore;

    public GetItemDefinitionByKeyQueryHandler(IItemDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetItemDefinitionByKeyResponse> Handle(
        GetItemDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        return new GetItemDefinitionByKeyResponse(
            await _readStore.GetDtoByKeyAsync(request.Key, cancellationToken));
    }
}
