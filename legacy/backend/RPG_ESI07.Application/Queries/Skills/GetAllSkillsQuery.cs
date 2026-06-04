using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Skills;

public record GetAllSkillsQuery : IRequest<GetAllSkillsResponse>;

public record GetAllSkillsResponse(List<Skill> Items);