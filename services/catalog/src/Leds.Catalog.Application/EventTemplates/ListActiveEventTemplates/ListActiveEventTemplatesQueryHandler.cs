using Leds.Catalog.Application.EventTemplates.Dtos;
using Leds.Catalog.Application.EventTemplates.Ports;
using MediatR;

namespace Leds.Catalog.Application.EventTemplates.ListActiveEventTemplates;

public sealed class ListActiveEventTemplatesQueryHandler
    : IRequestHandler<ListActiveEventTemplatesQuery, ListActiveEventTemplatesResponse>
{
    private readonly IEventTemplateReadStore _readStore;

    public ListActiveEventTemplatesQueryHandler(
        IEventTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveEventTemplatesResponse> Handle(
        ListActiveEventTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveEventTemplatesResponse(
            templates
                .Select(EventTemplateDto.FromDomain)
                .ToArray());
    }
}