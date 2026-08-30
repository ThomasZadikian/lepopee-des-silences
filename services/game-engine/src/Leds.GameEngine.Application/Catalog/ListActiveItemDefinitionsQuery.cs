using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record ListActiveItemDefinitionsQuery : IRequest<ListActiveItemDefinitionsResponse>;
