namespace Leds.GameEngine.Application.Catalog;

/// <summary>
/// A catalog room-type as seen by the run generator. The set of these defines the
/// Markov vocabulary the generator transitions over (Threshold, Memory, Rupture,
/// Silence, Fear…). Catalog is the source of truth: adding one extends the rotation.
/// </summary>
public sealed record CatalogRoomTypeDefinition(
    string Key,
    string DisplayName,
    string Theme,
    int MinDepth,
    int MaxDepth);
