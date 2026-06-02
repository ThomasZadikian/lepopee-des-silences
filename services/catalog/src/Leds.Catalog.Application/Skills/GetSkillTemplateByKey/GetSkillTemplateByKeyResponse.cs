using Leds.Catalog.Application.Skills.Dtos;

namespace Leds.Catalog.Application.Skills.GetSkillTemplateByKey;

public sealed record GetSkillTemplateByKeyResponse(
    SkillTemplateDto? Template);