using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Abstractions;

public interface IArchetypeDefinitionGateway
{
    Task<ArchetypeDefinitionSnapshot?> GetByKeyAsync(string key, CancellationToken cancellationToken);
}
