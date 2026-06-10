namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunMemoryFragmentEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string FragmentKey { get; set; } = string.Empty;
    public int Order { get; set; }

    public RunEntity? Run { get; set; }
}
