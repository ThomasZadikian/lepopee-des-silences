using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Skills;

public class DeleteSkillHandler : IRequestHandler<DeleteSkillCommand, DeleteSkillResponse>
{
    private readonly ISkillRepository _repository;

    public DeleteSkillHandler(ISkillRepository repository) => _repository = repository;

    public async Task<DeleteSkillResponse> Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id);
        return new DeleteSkillResponse(true, "Skill deleted successfully");
    }
}