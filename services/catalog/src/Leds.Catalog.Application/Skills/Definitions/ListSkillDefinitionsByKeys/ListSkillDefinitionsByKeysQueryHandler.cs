using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;

public sealed class ListSkillDefinitionsByKeysQueryHandler
    : IRequestHandler<ListSkillDefinitionsByKeysQuery, ListSkillDefinitionsByKeysResponse>
{
    private readonly ISkillDefinitionReadStore _readStore;

    public ListSkillDefinitionsByKeysQueryHandler(ISkillDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListSkillDefinitionsByKeysResponse> Handle(
        ListSkillDefinitionsByKeysQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _readStore.ListByKeysAsync(
            request.Keys, cancellationToken);

        return new ListSkillDefinitionsByKeysResponse(
            definitions
                .Select(SkillDefinitionDto.FromDomain)
                .ToArray());
    }
}
