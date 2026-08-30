using Leds.Catalog.Application.Skills.Definitions.Dtos;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;

public sealed record ListSkillDefinitionsByTypeResponse(
    IReadOnlyCollection<SkillDefinitionDto> Definitions);
