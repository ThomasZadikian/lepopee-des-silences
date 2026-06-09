using Leds.Catalog.Application.Skills.Definitions.Dtos;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;

public sealed record ListSkillDefinitionsByKeysResponse(
    IReadOnlyCollection<SkillDefinitionDto> Definitions);
