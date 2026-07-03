using Leds.Catalog.Domain.Rewards.Loot;

namespace Leds.Catalog.Application.Rewards.GenericLoot.Ports;

public interface IGenericLootPoolReadStore
{
    Task<IGenericLootPool?> GetActiveAsync(CancellationToken cancellationToken);
}
