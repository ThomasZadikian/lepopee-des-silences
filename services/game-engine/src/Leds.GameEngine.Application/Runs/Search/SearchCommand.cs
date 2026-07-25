using MediatR;

namespace Leds.GameEngine.Application.Runs.Search;

public sealed record SearchCommand(Guid RunId) : IRequest<SearchResponse>;
