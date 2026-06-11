namespace Leds.GameEngine.Infrastructure.Outbox;

public interface IPlayerProjectionClient
{
    Task SendRunOutcomeAsync(RunOutcomeProjectionEnvelope envelope, CancellationToken cancellationToken);
}
