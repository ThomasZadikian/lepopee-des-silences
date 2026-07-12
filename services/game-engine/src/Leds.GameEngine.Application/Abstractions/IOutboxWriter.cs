namespace Leds.GameEngine.Application.Abstractions;

public interface IOutboxWriter
{
    Task WriteAsync<T>(T integrationEvent, CancellationToken cancellationToken);
}
