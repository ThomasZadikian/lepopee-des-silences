namespace Leds.Player.Application.Abstractions;

public interface IAccountPrivacyMaintenanceStore
{
    Task<IReadOnlyCollection<Guid>> ListExecutableClosureAccountIdsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task PurgeAuthenticationMaterialAsync(
        Guid accountId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken);
}
