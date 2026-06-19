using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Application.Combats.Ports;

public interface ICombatInstanceRepository
{
    Task<CombatInstance?> GetByIdAsync(CombatId id, CancellationToken cancellationToken = default);
}
