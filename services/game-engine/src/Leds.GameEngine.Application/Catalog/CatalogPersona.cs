namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcPersona(
    string Tone,
    string Register,
    IReadOnlyCollection<string> Needs,
    IReadOnlyCollection<string> Offerings);