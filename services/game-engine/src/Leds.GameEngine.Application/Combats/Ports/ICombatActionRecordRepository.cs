using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Application.Combats.Ports;

public interface ICombatActionRecordRepository
{
    Task AddRangeAsync(IReadOnlyCollection<CombatActionRecord> records, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CombatActionRecord>> GetByCombatIdAsync(Guid combatId, CancellationToken cancellationToken);
}
