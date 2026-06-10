using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;

namespace Leds.Player.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPlayerProfileRepository : IPlayerProfileRepository
{
    private readonly Dictionary<Guid, PlayerProfile> _profiles = [];

    public Task<PlayerProfile?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken)
    {
        _profiles.TryGetValue(id.Value, out var profile);
        return Task.FromResult(profile);
    }

    public Task SaveAsync(PlayerProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Id.Value] = profile;
        return Task.CompletedTask;
    }
}
