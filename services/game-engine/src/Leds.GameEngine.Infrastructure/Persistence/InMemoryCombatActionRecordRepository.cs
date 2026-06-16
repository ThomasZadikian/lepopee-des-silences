using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Persistence;

public sealed class InMemoryCombatActionRecordRepository : ICombatActionRecordRepository
{
    private readonly List<CombatActionRecord> _records = [];

    public Task AddRangeAsync(IReadOnlyCollection<CombatActionRecord> records, CancellationToken cancellationToken)
    {
        _records.AddRange(records);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<CombatActionRecord>> GetByCombatIdAsync(Guid combatId, CancellationToken cancellationToken)
    {
        var results = _records
            .Where(r => r.CombatId == combatId)
            .OrderBy(r => r.TurnNumber)
            .ThenBy(r => r.OccurredAtUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CombatActionRecord>>(results);
    }
}
