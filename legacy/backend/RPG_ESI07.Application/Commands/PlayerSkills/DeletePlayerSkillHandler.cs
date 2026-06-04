using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerSkills;

public class DeletePlayerSkillHandler : IRequestHandler<DeletePlayerSkillCommand, DeletePlayerSkillResponse>
{
    private readonly IPlayerSkillRepository _repository;

    public DeletePlayerSkillHandler(IPlayerSkillRepository repository) => _repository = repository;

    public async Task<DeletePlayerSkillResponse> Handle(DeletePlayerSkillCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            throw new KeyNotFoundException("PlayerSkill not found");

        if (!request.IsAdmin && entity.PlayerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Access forbidden");

        await _repository.DeleteAsync(request.Id);
        return new DeletePlayerSkillResponse(true, "PlayerSkill deleted successfully");
    }
}