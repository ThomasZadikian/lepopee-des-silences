using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Ports;

public interface ICombatInstanceRepository
{
    Task AddAsync(CombatInstance combat, CancellationToken cancellationToken = default);

    Task<CombatInstance?> GetByIdAsync(CombatId combatId, CancellationToken cancellationToken = default);

    Task UpdateAsync(CombatInstance combat, CancellationToken cancellationToken = default);
}