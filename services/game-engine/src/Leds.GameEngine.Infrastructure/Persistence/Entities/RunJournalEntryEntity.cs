namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class RunJournalEntryEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }

    public RunEntity? Run { get; set; }
}
