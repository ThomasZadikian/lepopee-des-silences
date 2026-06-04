using MediatR;

namespace RPG_ESI07.Application.Commands.Enemies;
public record DeleteEnemyCommand(int Id) : IRequest<DeleteEnemyResponse>;
public record DeleteEnemyResponse(bool Success, string Message);