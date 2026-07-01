using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.Rewards.Loot;

// DropPercent is rolled independently per entry (a separate Bernoulli trial per item),
// not a relative weight among the other entries in the same table.
public sealed record LootEntry(string ItemDefinitionKey, int DropPercent)
{
    public string ItemDefinitionKey { get; } = string.IsNullOrWhiteSpace(ItemDefinitionKey)
        ? throw new DomainException("Loot entry item definition key is required.")
        : ItemDefinitionKey.Trim();

    public int DropPercent { get; } = DropPercent is >= 1 and <= 100
        ? DropPercent
        : throw new DomainException("Loot entry drop percent must be between 1 and 100.");
}
