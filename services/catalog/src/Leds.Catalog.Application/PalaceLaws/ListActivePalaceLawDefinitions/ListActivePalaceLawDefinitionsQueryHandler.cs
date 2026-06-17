using Leds.Catalog.Application.PalaceLaws.Dtos;
using Leds.Catalog.Application.PalaceLaws.Ports;
using MediatR;

namespace Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;

public sealed class ListActivePalaceLawDefinitionsQueryHandler
    : IRequestHandler<ListActivePalaceLawDefinitionsQuery, ListActivePalaceLawDefinitionsResponse>
{
    private readonly IPalaceLawDefinitionReadStore _readStore;

    public ListActivePalaceLawDefinitionsQueryHandler(
        IPalaceLawDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActivePalaceLawDefinitionsResponse> Handle(
        ListActivePalaceLawDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_readStore is IPalaceLawDefinitionDtoReadStore dtoReadStore)
        {
            var dtoDefinitions = await dtoReadStore.ListActiveDtosAsync(cancellationToken);
            return new ListActivePalaceLawDefinitionsResponse(dtoDefinitions.ToArray());
        }

        var definitions = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActivePalaceLawDefinitionsResponse(
            definitions
                .Select(PalaceLawDefinitionDto.FromDomain)
                .ToArray());
    }
}
