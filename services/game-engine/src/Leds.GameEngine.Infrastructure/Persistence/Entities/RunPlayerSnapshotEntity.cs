namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunPlayerSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid PlayerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public RunEntity? Run { get; set; }
    public List<RunCharacterSnapshotEntity> Characters { get; set; } = [];
}
