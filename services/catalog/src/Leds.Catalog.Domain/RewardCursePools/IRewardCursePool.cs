using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.RewardCursePools;

public interface IRewardCursePool : ICatalogContent
{
    IReadOnlyCollection<RewardCurseEntry> Entries { get; }
}