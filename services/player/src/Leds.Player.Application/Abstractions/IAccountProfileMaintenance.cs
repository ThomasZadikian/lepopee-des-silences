using Leds.Player.Domain.Players;

namespace Leds.Player.Application.Abstractions;

public interface IAccountProfileMaintenance
{
    Task RenameAsync(
        PlayerId accountId,
        string displayName,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken);

    Task AnonymizeAsync(
        PlayerId accountId,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken);
}
