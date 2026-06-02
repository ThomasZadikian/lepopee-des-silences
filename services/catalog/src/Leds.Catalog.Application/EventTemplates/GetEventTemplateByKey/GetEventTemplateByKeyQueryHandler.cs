using Leds.Catalog.Application.EventTemplates.Dtos;
using Leds.Catalog.Application.EventTemplates.Ports;
using MediatR;

namespace Leds.Catalog.Application.EventTemplates.GetEventTemplateByKey;

public sealed class GetEventTemplateByKeyQueryHandler
    : IRequestHandler<GetEventTemplateByKeyQuery, GetEventTemplateByKeyResponse>
{
    private readonly IEventTemplateReadStore _readStore;

    public GetEventTemplateByKeyQueryHandler(
        IEventTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetEventTemplateByKeyResponse> Handle(
        GetEventTemplateByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetEventTemplateByKeyResponse(
            template is null
                ? null
                : EventTemplateDto.FromDomain(template));
    }
}