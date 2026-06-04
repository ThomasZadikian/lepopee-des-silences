using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerSkills;

public class CreatePlayerSkillHandler : IRequestHandler<CreatePlayerSkillCommand, CreatePlayerSkillResponse>
{
    private readonly IPlayerSkillRepository _repository;

    public CreatePlayerSkillHandler(IPlayerSkillRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreatePlayerSkillResponse> Handle(CreatePlayerSkillCommand request, CancellationToken cancellationToken)
    {
        var entity = new PlayerSkill();
        await _repository.AddAsync(entity);
        return new CreatePlayerSkillResponse(entity.Id, "PlayerSkill created successfully");
    }
}