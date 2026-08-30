using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record ListAvailableCurseDefinitionsQuery : IRequest<ListAvailableCurseDefinitionsResponse>;
