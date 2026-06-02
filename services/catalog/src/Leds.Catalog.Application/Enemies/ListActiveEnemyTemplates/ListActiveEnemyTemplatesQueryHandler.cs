using Leds.Catalog.Application.Enemies.Dtos;
using Leds.Catalog.Application.Enemies.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.ListActiveEnemyTemplates;

public sealed class ListActiveEnemyTemplatesQueryHandler
    : IRequestHandler<ListActiveEnemyTemplatesQuery, ListActiveEnemyTemplatesResponse>
{
    private readonly IEnemyTemplateReadStore _readStore;

    public ListActiveEnemyTemplatesQueryHandler(IEnemyTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveEnemyTemplatesResponse> Handle(
        ListActiveEnemyTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveEnemyTemplatesResponse(
            templates.Select(EnemyTemplateDto.FromDomain).ToArray());
    }
}