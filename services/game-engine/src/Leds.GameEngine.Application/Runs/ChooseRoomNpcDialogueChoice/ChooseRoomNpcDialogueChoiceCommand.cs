using Leds.GameEngine.Application.Events.ChooseEventOption;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ChooseRoomNpcDialogueChoice;

public sealed record ChooseRoomNpcDialogueChoiceCommand(
    Guid RunId,
    Guid RoomNpcId,
    string ChoiceId) : IRequest<ChooseCurrentEventOptionResponse>;
