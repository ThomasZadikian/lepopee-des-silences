using Leds.Catalog.Application.Skills.Definitions.Dtos;
using Leds.Catalog.Application.Skills.Definitions.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;

public sealed class GetSkillDefinitionByKeyQueryHandler
    : IRequestHandler<GetSkillDefinitionByKeyQuery, GetSkillDefinitionByKeyResponse>
{
    private readonly ISkillDefinitionReadStore _readStore;

    public GetSkillDefinitionByKeyQueryHandler(ISkillDefinitionReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetSkillDefinitionByKeyResponse> Handle(
        GetSkillDefinitionByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _readStore.GetByKeyAsync(request.Key, cancellationToken);

        return new GetSkillDefinitionByKeyResponse(
            definition is null ? null : SkillDefinitionDto.FromDomain(definition));
    }
}
