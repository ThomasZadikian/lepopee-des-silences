using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Abstractions;

public interface IPlayerProfileRepository
{
    Task<PlayerProfile?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken);
    Task SaveAsync(PlayerProfile profile, CancellationToken cancellationToken);
}
