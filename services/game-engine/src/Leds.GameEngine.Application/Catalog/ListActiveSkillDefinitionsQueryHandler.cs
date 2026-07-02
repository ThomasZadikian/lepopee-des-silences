using Leds.GameEngine.Application.Catalog.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed class ListActiveSkillDefinitionsQueryHandler
    : IRequestHandler<ListActiveSkillDefinitionsQuery, ListActiveSkillDefinitionsResponse>
{
    private readonly ICatalogContentGateway _catalogGateway;

    public ListActiveSkillDefinitionsQueryHandler(ICatalogContentGateway catalogGateway)
    {
        _catalogGateway = catalogGateway;
    }

    public async Task<ListActiveSkillDefinitionsResponse> Handle(
        ListActiveSkillDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await _catalogGateway.ListActiveSkillDefinitionsAsync(cancellationToken);

        return new ListActiveSkillDefinitionsResponse(
            definitions.Select(d => new SkillDefinitionView(
                d.Key,
                d.DisplayName,
                d.Description,
                d.SkillType,
                d.TargetingType,
                d.EffectType,
                d.ManaCost,
                d.ChargeCost,
                d.BasePower)).ToArray());
    }
}
