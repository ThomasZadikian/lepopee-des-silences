using MediatR;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed record ChooseCurrentEventOptionCommand(
    Guid RunId,
    string ChoiceId) : IRequest<ChooseCurrentEventOptionResponse>;