using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.GetSkillTemplateByKey;

public sealed record GetSkillTemplateByKeyQuery(string Key)
    : IQuery<GetSkillTemplateByKeyResponse>;