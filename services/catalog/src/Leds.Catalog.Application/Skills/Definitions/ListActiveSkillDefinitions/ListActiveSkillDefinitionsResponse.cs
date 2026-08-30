using Leds.Catalog.Application.Skills.Definitions.Dtos;

namespace Leds.Catalog.Application.Skills.Definitions.ListActiveSkillDefinitions;

public sealed record ListActiveSkillDefinitionsResponse(
    IReadOnlyCollection<SkillDefinitionDto> Definitions);
