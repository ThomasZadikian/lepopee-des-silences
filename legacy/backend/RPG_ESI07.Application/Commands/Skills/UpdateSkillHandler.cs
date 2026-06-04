using MediatR;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Skills;

public class UpdateSkillHandler : IRequestHandler<UpdateSkillCommand, UpdateSkillResponse>
{
    private readonly ISkillRepository _repository;

    public UpdateSkillHandler(ISkillRepository repository) => _repository = repository;

    public async Task<UpdateSkillResponse> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null) return new UpdateSkillResponse(false, "Skill not found");

        entity.Name = request.Name;
        entity.EffectType = request.EffectType;
        entity.MPCost = request.MPCost;
        entity.ElementType = request.ElementType;
        entity.Description = request.Description;
        entity.BaseDamage = request.BaseDamage;
        entity.HealAmount = request.HealAmount;

        await _repository.UpdateAsync(entity);
        return new UpdateSkillResponse(true, "Skill updated successfully");
    }
}