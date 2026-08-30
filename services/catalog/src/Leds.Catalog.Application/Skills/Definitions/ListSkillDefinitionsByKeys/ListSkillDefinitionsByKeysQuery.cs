using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.Definitions.ListSkillDefinitionsByKeys;

public sealed record ListSkillDefinitionsByKeysQuery(
    IReadOnlyCollection<string> Keys)
    : IQuery<ListSkillDefinitionsByKeysResponse>;
