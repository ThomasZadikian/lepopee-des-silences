using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerSkills;

public class UpdatePlayerSkillHandler : IRequestHandler<UpdatePlayerSkillCommand, UpdatePlayerSkillResponse>
{
    private readonly IPlayerSkillRepository _repository;

    public UpdatePlayerSkillHandler(IPlayerSkillRepository repository) => _repository = repository;

    public async Task<UpdatePlayerSkillResponse> Handle(UpdatePlayerSkillCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("PlayerSkill not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        entity.PlayerId = request.PlayerId;
        entity.SkillId = request.SkillId;
        await _repository.UpdateAsync(entity);
        return new UpdatePlayerSkillResponse(true, "PlayerSkill updated successfully");
    }
}