using MediatR;

namespace RPG_ESI07.Application.Commands.Items;

public record DeleteItemCommand(int Id) : IRequest<DeleteItemResponse>;

public record DeleteItemResponse(bool Success, string Message);