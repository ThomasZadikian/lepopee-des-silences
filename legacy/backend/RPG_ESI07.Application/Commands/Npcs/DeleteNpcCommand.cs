using MediatR;

namespace RPG_ESI07.Application.Commands.Npcs;

public record DeleteNpcCommand(int Id) : IRequest<DeleteNpcResponse>;
public record DeleteNpcResponse(bool Success, string Message);