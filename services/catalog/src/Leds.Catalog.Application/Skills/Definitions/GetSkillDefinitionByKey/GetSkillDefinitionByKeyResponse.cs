using Leds.Catalog.Application.Skills.Definitions.Dtos;

namespace Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;

public sealed record GetSkillDefinitionByKeyResponse(
    SkillDefinitionDto? Definition);
