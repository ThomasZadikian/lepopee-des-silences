using MediatR;

namespace RPG_ESI07.Application.Commands.Skills;

public record DeleteSkillCommand(int Id) : IRequest<DeleteSkillResponse>;

public record DeleteSkillResponse(bool Success, string Message);