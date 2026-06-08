using Leds.GameEngine.Application.Interlude.Dtos;
using MediatR;

namespace Leds.GameEngine.Application.Interlude.EnterInterlude;

public sealed record EnterInterludeCommand(Guid RunId)
    : IRequest<EnterInterludeResponse>;
