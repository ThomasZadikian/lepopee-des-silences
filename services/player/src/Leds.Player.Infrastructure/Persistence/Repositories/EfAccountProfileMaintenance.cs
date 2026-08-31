using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Players;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence.Repositories;

public sealed class EfAccountProfileMaintenance : IAccountProfileMaintenance
{
    private readonly PlayerDbContext _context;

    public EfAccountProfileMaintenance(PlayerDbContext context)
    {
        _context = context;
    }

    public async Task RenameAsync(
        PlayerId accountId,
        string displayName,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Player display name is required.");

        var affected = await _context.PlayerProfiles
            .Where(profile => profile.Id == accountId.Value)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(profile => profile.DisplayName, displayName.Trim())
                    .SetProperty(profile => profile.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        if (affected != 1)
            throw new InvalidOperationException($"Account '{accountId.Value:D}' could not be renamed.");
    }

    public async Task AnonymizeAsync(
        PlayerId accountId,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var affected = await _context.PlayerProfiles
            .Where(profile => profile.Id == accountId.Value)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(profile => profile.DisplayName, "Compte anonymisé")
                    .SetProperty(profile => profile.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        if (affected != 1)
            throw new InvalidOperationException($"Account '{accountId.Value:D}' could not be anonymised.");

        await _context.PlayerCharacters
            .Where(character => character.PlayerProfileId == accountId.Value && character.CharacterType == "Player")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(character => character.DisplayName, "Personnage anonymisé")
                    .SetProperty(character => character.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
