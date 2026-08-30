using MediatR;

namespace Leds.GameEngine.Application.Runs.RemovePalaceLaw;

public sealed record RemovePalaceLawCommand(Guid RunId, string LawKey)
    : IRequest<RemovePalaceLawResponse>;
