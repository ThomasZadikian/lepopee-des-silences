using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Skills.ListActiveSkillTemplates;

public sealed record ListActiveSkillTemplatesQuery()
    : IQuery<ListActiveSkillTemplatesResponse>;