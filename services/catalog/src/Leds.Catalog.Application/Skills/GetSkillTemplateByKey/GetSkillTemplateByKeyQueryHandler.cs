using Leds.Catalog.Application.Skills.Dtos;
using Leds.Catalog.Application.Skills.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.GetSkillTemplateByKey;

public sealed class GetSkillTemplateByKeyQueryHandler
    : IRequestHandler<GetSkillTemplateByKeyQuery, GetSkillTemplateByKeyResponse>
{
    private readonly ISkillTemplateReadStore _readStore;

    public GetSkillTemplateByKeyQueryHandler(ISkillTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<GetSkillTemplateByKeyResponse> Handle(
        GetSkillTemplateByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _readStore.GetByKeyAsync(
            request.Key,
            cancellationToken);

        return new GetSkillTemplateByKeyResponse(
            template is null ? null : SkillTemplateDto.FromDomain(template));
    }
}