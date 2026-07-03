namespace Leds.Catalog.Application.Worlds.Dtos;

public sealed record WorldDefinitionDto(
    Guid Id, string Key, string DisplayName, string EntryRoomKey, string Version, string Status);
