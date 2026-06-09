using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByType;

public sealed record ListSkillDefinitionsByTypeQuery(string SkillType)
    : IQuery<ListSkillDefinitionsByTypeResponse>;
