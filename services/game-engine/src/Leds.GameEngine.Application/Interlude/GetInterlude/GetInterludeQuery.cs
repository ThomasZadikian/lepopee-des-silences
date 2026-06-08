using Leds.GameEngine.Application.Interlude.Dtos;
using MediatR;

namespace Leds.GameEngine.Application.Interlude.GetInterlude;

public sealed record GetInterludeQuery(Guid RunId)
    : IRequest<GetInterludeResponse>;
