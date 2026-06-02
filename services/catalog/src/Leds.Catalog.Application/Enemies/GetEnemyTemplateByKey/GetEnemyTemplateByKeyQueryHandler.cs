using Leds.Catalog.Application.Enemies.Dtos;
using Leds.Catalog.Application.Enemies.Ports;
using MediatR;

namespace Leds.Catalog.Application.Enemies.GetEnemyTemplateByKey;

public sealed class GetEnemyTemplateByKeyQueryHandler
    : IRequestHandler<GetEnemyTemplateByKeyQuery, GetEnemyTemplateByKeyResponse>
{
    private readonly IEnemyTemplateReadStore _readStore;

    public GetEnemyTemplateByKeyQueryHandler(IEnemyTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetEnemyTemplateByKeyResponse> Handle(
        GetEnemyTemplateByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetEnemyTemplateByKeyResponse(
            template is null ? null : EnemyTemplateDto.FromDomain(template));
    }
}