using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Application.PalaceLaws.Ports;
using MediatR;

namespace Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;

public sealed class GetPalaceLawDefinitionByKeyQueryHandler
    : IRequestHandler<GetPalaceLawDefinitionByKeyQuery, GetPalaceLawDefinitionByKeyResponse>
{
    private readonly IPalaceLawDefinitionReadStore _readStore;

    public GetPalaceLawDefinitionByKeyQueryHandler(
        IPalaceLawDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetPalaceLawDefinitionByKeyResponse> Handle(
        GetPalaceLawDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetPalaceLawDefinitionByKeyResponse(
            definition is null
                ? null
                : PalaceLawDefinitionDto.FromDomain(definition));
    }
}