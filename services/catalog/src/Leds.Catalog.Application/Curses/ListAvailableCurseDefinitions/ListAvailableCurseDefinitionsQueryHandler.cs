using Leds.Catalog.Application.Curses.Dtos;
using Leds.Catalog.Application.Curses.Ports;
using MediatR;

namespace Leds.Catalog.Application.Curses.ListAvailableCurseDefinitions;

public sealed class ListAvailableCurseDefinitionsQueryHandler
    : IRequestHandler<ListAvailableCurseDefinitionsQuery, ListAvailableCurseDefinitionsResponse>
{
    private readonly ICurseDefinitionReadStore _readStore;

    public ListAvailableCurseDefinitionsQueryHandler(ICurseDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListAvailableCurseDefinitionsResponse> Handle(
        ListAvailableCurseDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_readStore is ICurseDefinitionDtoReadStore dtoReadStore)
        {
            var dtoDefinitions = await dtoReadStore.ListAvailableDtosAsync(cancellationToken);
            return new ListAvailableCurseDefinitionsResponse(dtoDefinitions.ToArray());
        }

        var definitions = await _readStore.ListAvailableAsync(cancellationToken);
        return new ListAvailableCurseDefinitionsResponse(
            definitions.Select(CurseDefinitionDto.FromDomain).ToArray());
    }
}
