namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunActivePalaceLawEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid LawId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Domains { get; set; } = string.Empty;

    public RunEntity? Run { get; set; }
}