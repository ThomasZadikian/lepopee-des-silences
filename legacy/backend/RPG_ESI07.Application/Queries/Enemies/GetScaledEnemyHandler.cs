using MediatR;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Queries.Enemies;

public class GetScaledEnemyHandler : IRequestHandler<GetScaledEnemyQuery, GetScaledEnemyResponse>
{
    private readonly IEnemyRepository _repository;

    public GetScaledEnemyHandler(IEnemyRepository repository) => _repository = repository;

    public async Task<GetScaledEnemyResponse> Handle(GetScaledEnemyQuery request, CancellationToken ct)
    {
        var enemy = await _repository.GetByIdAsync(request.EnemyId);
        if (enemy == null) return new GetScaledEnemyResponse(null);

        var multiplier = CalculateMultiplier(enemy.Type, request.PlayerLevel, request.EquipmentBonus);

        var scaled = new ScaledEnemyDto(
            enemy.Id,
            enemy.Name,
            enemy.Type,
            (int)(enemy.MaxHP * multiplier),
            (int)(enemy.Strength * multiplier),
            (int)(enemy.Intelligence * multiplier),
            (int)(enemy.Speed * multiplier),
            enemy.PhysicalResistance,
            enemy.MagicalResistance,
            (int)(enemy.ExperienceReward * multiplier),
            (int)(enemy.GoldReward * multiplier),
            enemy.Description,
            enemy.TransitionMatrix,
            enemy.CombatScripts,
            enemy.MapStates,
            enemy.InitialState,
            multiplier
        );

        return new GetScaledEnemyResponse(scaled);
    }

    private static float CalculateMultiplier(string enemyType, int playerLevel, int equipmentBonus)
    {
        // Paramètres par type
        float basepower, maxMult, k;

        switch (enemyType)
        {
            case Constants.EnemyTypeBoss:
                basepower = 400f; maxMult = 20f; k = 1.5f;
                break;
            case Constants.EnemyTypeMiniboss:
                basepower = 150f; maxMult = 12f; k = 2.0f;
                break;
            default: // basic
                basepower = 50f; maxMult = 8f; k = 2.5f;
                break;
        }

        // Puissance joueur = niveau * 10 + bonus équipement
        float playerPower = (playerLevel * 10f) + equipmentBonus;

        // Ratio joueur/ennemi
        float ratio = playerPower / basepower;

        // S-curve logistique
        float multiplier = 1f + (maxMult - 1f) / (1f + MathF.Exp(-k * (ratio - 1f)));

        return multiplier;
    }
}