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
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Duration { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public Guid? ExpiresAtRoomId { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public string Rarity { get; set; } = "Commun";
    public string Polarity { get; set; } = "Neutre";
    public bool IsMajeure { get; set; }
    public string? RoomKey { get; set; }
    public bool IsCumulExempt { get; set; }

    public RunEntity? Run { get; set; }
}
