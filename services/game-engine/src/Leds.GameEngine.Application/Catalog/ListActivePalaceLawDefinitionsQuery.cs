using MediatR;

namespace Leds.GameEngine.Application.Catalog;

public sealed record ListActivePalaceLawDefinitionsQuery : IRequest<ListActivePalaceLawDefinitionsResponse>;
