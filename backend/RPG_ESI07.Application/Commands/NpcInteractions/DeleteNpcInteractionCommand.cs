using MediatR;

namespace RPG_ESI07.Application.Commands.NpcInteractions;

public record DeleteNpcInteractionCommand(int Id) : IRequest<DeleteNpcInteractionResponse>;
public record DeleteNpcInteractionResponse(bool Success, string Message);