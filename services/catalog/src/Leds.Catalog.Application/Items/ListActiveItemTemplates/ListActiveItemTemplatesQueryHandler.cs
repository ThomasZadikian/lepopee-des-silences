using Leds.Catalog.Application.Items.Dtos;
using Leds.Catalog.Application.Items.Ports;
using MediatR;

namespace Leds.Catalog.Application.Items.ListActiveItemTemplates;

public sealed class ListActiveItemTemplatesQueryHandler
    : IRequestHandler<ListActiveItemTemplatesQuery, ListActiveItemTemplatesResponse>
{
    private readonly IItemTemplateReadStore _readStore;

    public ListActiveItemTemplatesQueryHandler(IItemTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveItemTemplatesResponse> Handle(
        ListActiveItemTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveItemTemplatesResponse(
            templates.Select(ItemTemplateDto.FromDomain).ToArray());
    }
}