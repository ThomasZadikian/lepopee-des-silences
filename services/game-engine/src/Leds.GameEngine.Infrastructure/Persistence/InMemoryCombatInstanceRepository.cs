using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Persistence;

public sealed class InMemoryCombatInstanceRepository : ICombatInstanceRepository
{
    private readonly Dictionary<CombatId, CombatInstance> _combats = [];

    public Task AddAsync(CombatInstance combat, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(combat);

        _combats.Add(combat.Id, combat);

        return Task.CompletedTask;
    }

    public Task<CombatInstance?> GetByIdAsync(CombatId combatId, CancellationToken cancellationToken = default)
    {
        _combats.TryGetValue(combatId, out var combat);

        return Task.FromResult(combat);
    }

    public Task UpdateAsync(CombatInstance combat, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(combat);

        _combats[combat.Id] = combat;

        return Task.CompletedTask;
    }
}