using Leds.Catalog.Application.Items.Dtos;
using Leds.Catalog.Application.Items.Ports;
using MediatR;

namespace Leds.Catalog.Application.Items.GetItemTemplateByKey;

public sealed class GetItemTemplateByKeyQueryHandler
    : IRequestHandler<GetItemTemplateByKeyQuery, GetItemTemplateByKeyResponse>
{
    private readonly IItemTemplateReadStore _readStore;

    public GetItemTemplateByKeyQueryHandler(IItemTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetItemTemplateByKeyResponse> Handle(
        GetItemTemplateByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetItemTemplateByKeyResponse(
            template is null ? null : ItemTemplateDto.FromDomain(template));
    }
}