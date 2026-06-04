using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.PlayerSkills;

public record GetPlayerSkillByIdQuery(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<GetPlayerSkillByIdResponse>;

public record GetPlayerSkillByIdResponse(PlayerSkill PlayerSkill);