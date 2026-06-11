namespace Leds.GameEngine.Application.Players.Ports;

public interface IPlayerRunSnapshotGateway
{
    Task<PlayerRunSnapshot> GetRunSnapshotAsync(Guid playerId, CancellationToken cancellationToken);
}
