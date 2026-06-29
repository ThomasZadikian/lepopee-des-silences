namespace Leds.Catalog.Application.RoomTypes.Dtos;

/// <summary>
/// Read model for a catalog room-type definition — the vocabulary the run generator's
/// Markov chain transitions over (e.g. Threshold, Memory, Rupture, Silence, Fear…).
/// Adding a room type in the catalog adds it to the rotation; nothing is hardcoded.
/// </summary>
public sealed record RoomTypeDefinitionDto(
    Guid Id,
    string Key,
    string DisplayName,
    string Description,
    string Theme,
    string DefaultRarity,
    int? MinDepth,
    int? MaxDepth,
    string Version,
    string Status);
