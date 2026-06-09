using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;

public sealed class ListSkillDefinitionsByTypeQueryHandler
    : IRequestHandler<ListSkillDefinitionsByTypeQuery, ListSkillDefinitionsByTypeResponse>
{
    private readonly ISkillDefinitionReadStore _readStore;

    public ListSkillDefinitionsByTypeQueryHandler(ISkillDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListSkillDefinitionsByTypeResponse> Handle(
        ListSkillDefinitionsByTypeQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListByTypeAsync(
            request.SkillType, cancellationToken);

        return new ListSkillDefinitionsByTypeResponse(
            definitions
                .Select(SkillDefinitionDto.FromDomain)
                .ToArray());
    }
}
