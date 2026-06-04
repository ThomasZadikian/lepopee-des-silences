using MediatR;

namespace RPG_ESI07.Application.Commands.NpcInteractions;

public record CreateNpcInteractionCommand(
    int NpcId,
    int PlayerId,
    string EventType
) : IRequest<CreateNpcInteractionResponse>;

public record CreateNpcInteractionResponse(int Id, string Message);