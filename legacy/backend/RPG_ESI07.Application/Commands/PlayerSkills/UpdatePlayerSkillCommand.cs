using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerSkills;

public record UpdatePlayerSkillCommand(
    int Id, int PlayerId, int SkillId,
    int RequestingUserId, bool IsAdmin
) : IRequest<UpdatePlayerSkillResponse>;

public record UpdatePlayerSkillResponse(bool Success, string Message);