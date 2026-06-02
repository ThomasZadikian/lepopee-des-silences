using Leds.Catalog.Application.Skills.Dtos;

namespace Leds.Catalog.Application.Skills.ListActiveSkillTemplates;

public sealed record ListActiveSkillTemplatesResponse(
    IReadOnlyCollection<SkillTemplateDto> Templates);