using MediatR;

namespace Leds.GameEngine.Application.Runs.GetPermanentItemCandidates;

public sealed record GetPermanentItemCandidatesQuery(Guid RunId)
    : IRequest<GetPermanentItemCandidatesResponse>;
