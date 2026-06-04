using MediatR;

namespace RPG_ESI07.Application.Commands.GameSaves;

public record CreateGameSaveCommand(
    int PlayerId, string CurrentZone, int PlayerLevel
) : IRequest<CreateGameSaveResponse>;

public record CreateGameSaveResponse(int Id, string Message);