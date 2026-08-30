using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.Definitions.GetSkillDefinitionByKey;

public sealed record GetSkillDefinitionByKeyQuery(string Key)
    : IQuery<GetSkillDefinitionByKeyResponse>;
