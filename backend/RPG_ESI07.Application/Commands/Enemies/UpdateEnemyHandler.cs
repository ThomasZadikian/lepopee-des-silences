using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.Enemies;

public class UpdateEnemyHandler : IRequestHandler<UpdateEnemyCommand, UpdateEnemyResponse>
{
    private readonly IEnemyRepository _repository;
    public UpdateEnemyHandler(IEnemyRepository repository) => _repository = repository;

    public async Task<UpdateEnemyResponse> Handle(UpdateEnemyCommand request, CancellationToken ct)
    {
        var entity = new Enemy
        {
            Id = request.Id,
            Name = request.Name,
            Type = request.Type,
            MaxHP = request.MaxHP,
            Strength = request.Strength,
            Intelligence = request.Intelligence,
            Speed = request.Speed,
            PhysicalResistance = request.PhysicalResistance,
            MagicalResistance = request.MagicalResistance,
            ExperienceReward = request.ExperienceReward,
            GoldReward = request.GoldReward,
            Description = request.Description,
            InfluenceRadius = request.InfluenceRadius,
            TransitionMatrix = request.TransitionMatrix,
            CombatScripts = request.CombatScripts,
            MapStates = request.MapStates,
            InitialState = request.InitialState
        };
        await _repository.UpdateAsync(entity);
        return new UpdateEnemyResponse(true, "Enemy updated successfully");
    }
}