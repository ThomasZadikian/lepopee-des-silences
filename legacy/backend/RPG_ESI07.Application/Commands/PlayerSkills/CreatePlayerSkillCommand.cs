using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerSkills;

public record CreatePlayerSkillCommand(
    int PlayerId, int SkillId, int SkillLevel
) : IRequest<CreatePlayerSkillResponse>;

public record CreatePlayerSkillResponse(int Id, string Message);