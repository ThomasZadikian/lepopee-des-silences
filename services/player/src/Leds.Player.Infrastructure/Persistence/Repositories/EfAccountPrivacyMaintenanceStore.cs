using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence.Repositories;

public sealed class EfAccountPrivacyMaintenanceStore : IAccountPrivacyMaintenanceStore
{
    private readonly PlayerDbContext _context;

    public EfAccountPrivacyMaintenanceStore(PlayerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Guid>> ListExecutableClosureAccountIdsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await _context.AccountIdentities
            .AsNoTracking()
            .Where(identity => identity.ClosureRequestedAtUtc != null
                && identity.ClosureExecuteAfterUtc != null
                && identity.ClosureExecuteAfterUtc <= now
                && identity.ClosureCancelledAtUtc == null
                && !EF.Functions.Like(identity.Email, "%@deleted.invalid"))
            .Select(identity => identity.AccountId)
            .ToArrayAsync(cancellationToken);

    public async Task PurgeAuthenticationMaterialAsync(
        Guid accountId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var identity = await _context.AccountIdentities
            .SingleOrDefaultAsync(value => value.AccountId == accountId, cancellationToken);
        if (identity is null)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        identity.Email = $"deleted-{accountId:N}@deleted.invalid";
        identity.PasswordHash = $"disabled:{Guid.NewGuid():N}";
        identity.Role = (int)AccountRole.Player;
        identity.EmailVerifiedAtUtc = null;
        identity.MfaSecretProtected = null;
        identity.MfaConfiguredAtUtc = null;
        identity.RecoveryCodeHashesJson = "[]";

        await _context.AccountPrivacyConsents
            .Where(consent => consent.AccountId == accountId && consent.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(consent => consent.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

        await _context.AccountSecurityTokens
            .Where(token => token.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        await _context.AccountSessions
            .Where(session => session.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        await _context.ActiveGameSessionLeases
            .Where(lease => lease.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
