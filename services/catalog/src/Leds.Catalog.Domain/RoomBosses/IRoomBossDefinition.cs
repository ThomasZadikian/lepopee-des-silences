using Leds.Catalog.Domain.Abstractions;

namespace Leds.Catalog.Domain.RoomBosses;

public interface IRoomBossDefinition : ICatalogContent
{
    string RoomType { get; }

    int BaseDifficulty { get; }

    IReadOnlyCollection<string> Tags { get; }
}
