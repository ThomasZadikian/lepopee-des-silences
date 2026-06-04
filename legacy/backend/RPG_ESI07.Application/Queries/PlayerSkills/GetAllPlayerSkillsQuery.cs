using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.PlayerSkills;

public record GetAllPlayerSkillsQuery(int? UserId = null) : IRequest<GetAllPlayerSkillsResponse>;
public record GetAllPlayerSkillsResponse(List<PlayerSkill> Items);