using MediatR;

namespace Leds.GameEngine.Application.Runs.AbandonRun;

public sealed record AbandonRunCommand(Guid RunId) : IRequest<AbandonRunResponse>;