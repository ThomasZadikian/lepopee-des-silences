using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.Definitions.ListActiveSkillDefinitions;

public sealed record ListActiveSkillDefinitionsQuery()
    : IQuery<ListActiveSkillDefinitionsResponse>;
