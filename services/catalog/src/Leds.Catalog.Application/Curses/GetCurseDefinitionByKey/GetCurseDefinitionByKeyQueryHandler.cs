using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.Ports;
using MediatR;

namespace Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;

public sealed class GetCurseDefinitionByKeyQueryHandler
    : IRequestHandler<GetCurseDefinitionByKeyQuery, GetCurseDefinitionByKeyResponse>
{
    private readonly ICurseDefinitionReadStore _readStore;

    public GetCurseDefinitionByKeyQueryHandler(ICurseDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetCurseDefinitionByKeyResponse> Handle(
        GetCurseDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        if (_readStore is ICurseDefinitionDtoReadStore dtoReadStore)
        {
            return new GetCurseDefinitionByKeyResponse(
                await dtoReadStore.GetDtoByKeyAsync(request.Key, cancellationToken));
        }

        var definition = await _readStore.GetByKeyAsync(request.Key, cancellationToken);
        return new GetCurseDefinitionByKeyResponse(
            definition is null ? null : CurseDefinitionDto.FromDomain(definition));
    }
}
