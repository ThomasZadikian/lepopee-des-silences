using Leds.Catalog.Domain.RoomBosses;

namespace Leds.Catalog.Application.RoomBosses.Dtos;

public sealed record RoomBossDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    string RoomType,
    int BaseDifficulty,
    IReadOnlyCollection<string> Tags)
{
    public static RoomBossDefinitionDto FromDomain(
        IRoomBossDefinition definition)
    {
        return new RoomBossDefinitionDto(
            definition.Id.Value,
            definition.Key.Value,
            definition.Name.Value,
            definition.Description.Value,
            definition.Version.Value,
            definition.Status.ToString(),
            definition.RoomType,
            definition.BaseDifficulty,
            definition.Tags.ToArray());
    }
}
