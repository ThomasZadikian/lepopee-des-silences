using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public class CreateCombatStatsHandler : IRequestHandler<CreateCombatStatsCommand, CreateCombatStatsResponse>
{
    private readonly ICombatStatsRepository _repository;

    public CreateCombatStatsHandler(ICombatStatsRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateCombatStatsResponse> Handle(CreateCombatStatsCommand request, CancellationToken cancellationToken)
    {
        var entity = new CombatStats();
        await _repository.AddAsync(entity);
        return new CreateCombatStatsResponse(entity.Id, "CombatStats created successfully");
    }
}