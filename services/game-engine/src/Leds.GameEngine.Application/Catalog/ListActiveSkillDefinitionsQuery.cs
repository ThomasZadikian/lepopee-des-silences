using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record ListActiveSkillDefinitionsQuery : IRequest<ListActiveSkillDefinitionsResponse>;
