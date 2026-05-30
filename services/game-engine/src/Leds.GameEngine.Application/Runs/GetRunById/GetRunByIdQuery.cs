using MediatR;

namespace Leds.GameEngine.Application.Runs.GetRunById;

public sealed record GetRunByIdQuery(Guid RunId) : IRequest<GetRunByIdResponse>;