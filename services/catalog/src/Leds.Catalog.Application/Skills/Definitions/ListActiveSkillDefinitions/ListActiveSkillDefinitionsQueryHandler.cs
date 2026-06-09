using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.Definitions.ListActiveSkillDefinitions;

public sealed class ListActiveSkillDefinitionsQueryHandler
    : IRequestHandler<ListActiveSkillDefinitionsQuery, ListActiveSkillDefinitionsResponse>
{
    private readonly ISkillDefinitionReadStore _readStore;

    public ListActiveSkillDefinitionsQueryHandler(ISkillDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveSkillDefinitionsResponse> Handle(
        ListActiveSkillDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveSkillDefinitionsResponse(
            definitions
                .Select(SkillDefinitionDto.FromDomain)
                .ToArray());
    }
}
