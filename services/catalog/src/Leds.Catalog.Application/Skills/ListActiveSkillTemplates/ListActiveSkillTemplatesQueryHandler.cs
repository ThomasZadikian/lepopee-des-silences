using Leds.Catalog.Application.Skills.Dtos;
using Leds.Catalog.Application.Skills.Ports;
using MediatR;

namespace Leds.Catalog.Application.Skills.ListActiveSkillTemplates;

public sealed class ListActiveSkillTemplatesQueryHandler
    : IRequestHandler<ListActiveSkillTemplatesQuery, ListActiveSkillTemplatesResponse>
{
    private readonly ISkillTemplateReadStore _readStore;

    public ListActiveSkillTemplatesQueryHandler(ISkillTemplateReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<ListActiveSkillTemplatesResponse> Handle(
        ListActiveSkillTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _readStore.ListActiveAsync(cancellationToken);

        return new ListActiveSkillTemplatesResponse(
            templates.Select(SkillTemplateDto.FromDomain).ToArray());
    }
}