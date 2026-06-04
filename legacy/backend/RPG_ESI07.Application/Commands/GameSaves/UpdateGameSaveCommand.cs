using MediatR;

namespace RPG_ESI07.Application.Commands.GameSaves;

public record UpdateGameSaveCommand(
    int Id, int PlayerId, string CurrentZone, int PlayerLevel,
    int RequestingUserId, bool IsAdmin
) : IRequest<UpdateGameSaveResponse>;

public record UpdateGameSaveResponse(bool Success, string Message);