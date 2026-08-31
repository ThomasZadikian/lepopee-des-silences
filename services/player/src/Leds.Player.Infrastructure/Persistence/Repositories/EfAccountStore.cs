using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Privacy;
using Leds.Player.Domain.Sessions;
using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.Player.Infrastructure.Persistence.Repositories;

public sealed class EfAccountStore : IAccountStore
{
    private readonly PlayerDbContext _context;

    public EfAccountStore(PlayerDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(EmailAddress email, CancellationToken cancellationToken) =>
        _context.AccountIdentities.AnyAsync(x => x.Email == email.Value, cancellationToken);

    public async Task<UserIdentity?> FindIdentityByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountIdentities
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email.Value, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<UserIdentity?> FindIdentityByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountIdentities
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task RegisterAsync(
        PlayerProfile profile,
        UserIdentity identity,
        string verificationTokenHash,
        DateTimeOffset verificationTokenExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await new EfPlayerProfileRepository(_context).SaveAsync(profile, cancellationToken);
        _context.AccountIdentities.Add(ToEntity(identity));
        _context.AccountSecurityTokens.Add(new SecurityTokenEntity
        {
            Id = Guid.NewGuid(),
            AccountId = identity.AccountId,
            Purpose = "email-verification",
            TokenHash = verificationTokenHash,
            IssuedAtUtc = identity.CreatedAtUtc,
            ExpiresAtUtc = verificationTokenExpiresAtUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveIdentityAsync(UserIdentity identity, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountIdentities
            .SingleAsync(x => x.Id == identity.Id, cancellationToken);

        entity.Email = identity.Email.Value;
        entity.PasswordHash = identity.PasswordHash;
        entity.Role = (int)identity.Role;
        entity.EmailVerifiedAtUtc = identity.EmailVerifiedAtUtc;
        entity.MfaSecretProtected = identity.MfaSecretProtected;
        entity.MfaConfiguredAtUtc = identity.MfaConfiguredAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task StoreSecurityTokenAsync(
        Guid accountId,
        string purpose,
        string tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        _context.AccountSecurityTokens.Add(new SecurityTokenEntity
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Purpose = purpose,
            TokenHash = tokenHash,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SecurityTokenState?> FindSecurityTokenAsync(
        string purpose,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountSecurityTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Purpose == purpose && x.TokenHash == tokenHash,
                cancellationToken);

        return entity is null
            ? null
            : new SecurityTokenState(
                entity.Id,
                entity.AccountId,
                entity.Purpose,
                entity.TokenHash,
                entity.IssuedAtUtc,
                entity.ExpiresAtUtc,
                entity.ConsumedAtUtc);
    }

    public async Task ConsumeSecurityTokenAsync(
        Guid tokenId,
        DateTimeOffset consumedAtUtc,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountSecurityTokens
            .SingleAsync(x => x.Id == tokenId, cancellationToken);
        if (!entity.ConsumedAtUtc.HasValue)
        {
            entity.ConsumedAtUtc = consumedAtUtc;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddSessionAsync(AccountSession session, CancellationToken cancellationToken)
    {
        _context.AccountSessions.Add(ToEntity(session));
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyCollection<AccountSession>> ListSessionsAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entities = await _context.AccountSessions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    public async Task SaveSessionAsync(AccountSession session, CancellationToken cancellationToken)
    {
        var entity = await _context.AccountSessions
            .SingleAsync(x => x.SessionId == session.SessionId, cancellationToken);
        entity.RefreshTokenHash = session.RefreshTokenHash;
        entity.ExpiresAtUtc = session.ExpiresAtUtc;
        entity.RotatedAtUtc = session.RotatedAtUtc;
        entity.RevokedAtUtc = session.RevokedAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeSessionsAsync(
        Guid accountId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var sessions = await _context.AccountSessions
            .Where(x => x.AccountId == accountId && x.RevokedAtUtc == null)
            .ToArrayAsync(cancellationToken);
        foreach (var session in sessions)
            session.RevokedAtUtc = revokedAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PrivacyConsent>> ListConsentsAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entities = await _context.AccountPrivacyConsents
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.GrantedAtUtc)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(x => PrivacyConsent.Rehydrate(
                x.PurposeKey,
                x.PolicyVersion,
                x.GrantedAtUtc,
                x.RevokedAtUtc))
            .ToArray();
    }

    public async Task SaveConsentAsync(
        Guid accountId,
        PrivacyConsent consent,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountPrivacyConsents.SingleOrDefaultAsync(
            x => x.AccountId == accountId
                && x.PurposeKey == consent.PurposeKey
                && x.PolicyVersion == consent.PolicyVersion
                && x.GrantedAtUtc == consent.GrantedAtUtc,
            cancellationToken);

        if (entity is null)
        {
            _context.AccountPrivacyConsents.Add(new PrivacyConsentEntity
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PurposeKey = consent.PurposeKey,
                PolicyVersion = consent.PolicyVersion,
                GrantedAtUtc = consent.GrantedAtUtc,
                RevokedAtUtc = consent.RevokedAtUtc
            });
        }
        else
        {
            entity.RevokedAtUtc = consent.RevokedAtUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountClosureRequest?> GetClosureRequestAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountIdentities
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

        if (entity?.ClosureRequestedAtUtc is null || entity.ClosureExecuteAfterUtc is null)
            return null;

        return AccountClosureRequest.Rehydrate(
            accountId,
            entity.ClosureRequestedAtUtc.Value,
            entity.ClosureExecuteAfterUtc.Value,
            entity.ClosureCancelledAtUtc);
    }

    public async Task SaveClosureRequestAsync(
        AccountClosureRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.AccountIdentities
            .SingleAsync(x => x.AccountId == request.AccountId, cancellationToken);
        entity.ClosureRequestedAtUtc = request.RequestedAtUtc;
        entity.ClosureExecuteAfterUtc = request.ExecuteAfterUtc;
        entity.ClosureCancelledAtUtc = request.CancelledAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ActiveGameSessionLease?> GetGameLeaseAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.ActiveGameSessionLeases
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        return entity is null
            ? null
            : ActiveGameSessionLease.Rehydrate(
                entity.AccountId,
                entity.OwnerSessionId,
                entity.AcquiredAtUtc,
                entity.ExpiresAtUtc);
    }

    public async Task SaveGameLeaseAsync(
        ActiveGameSessionLease lease,
        CancellationToken cancellationToken)
    {
        var entity = await _context.ActiveGameSessionLeases
            .SingleOrDefaultAsync(x => x.AccountId == lease.AccountId, cancellationToken);
        if (entity is null)
        {
            _context.ActiveGameSessionLeases.Add(new ActiveGameSessionLeaseEntity
            {
                AccountId = lease.AccountId,
                OwnerSessionId = lease.OwnerSessionId,
                AcquiredAtUtc = lease.AcquiredAtUtc,
                ExpiresAtUtc = lease.ExpiresAtUtc
            });
        }
        else
        {
            entity.OwnerSessionId = lease.OwnerSessionId;
            entity.AcquiredAtUtc = lease.AcquiredAtUtc;
            entity.ExpiresAtUtc = lease.ExpiresAtUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static AccountIdentityEntity ToEntity(UserIdentity identity) => new()
    {
        Id = identity.Id,
        AccountId = identity.AccountId,
        Email = identity.Email.Value,
        PasswordHash = identity.PasswordHash,
        Role = (int)identity.Role,
        CreatedAtUtc = identity.CreatedAtUtc,
        EmailVerifiedAtUtc = identity.EmailVerifiedAtUtc,
        MfaSecretProtected = identity.MfaSecretProtected,
        MfaConfiguredAtUtc = identity.MfaConfiguredAtUtc
    };

    private static UserIdentity ToDomain(AccountIdentityEntity entity) =>
        UserIdentity.Rehydrate(
            entity.Id,
            entity.AccountId,
            EmailAddress.Create(entity.Email),
            entity.PasswordHash,
            (AccountRole)entity.Role,
            entity.CreatedAtUtc,
            entity.EmailVerifiedAtUtc,
            entity.MfaSecretProtected,
            entity.MfaConfiguredAtUtc);

    private static AccountSessionEntity ToEntity(AccountSession session) => new()
    {
        SessionId = session.SessionId,
        AccountId = session.AccountId,
        RefreshTokenHash = session.RefreshTokenHash,
        CreatedAtUtc = session.CreatedAtUtc,
        ExpiresAtUtc = session.ExpiresAtUtc,
        RotatedAtUtc = session.RotatedAtUtc,
        RevokedAtUtc = session.RevokedAtUtc
    };

    private static AccountSession ToDomain(AccountSessionEntity entity) =>
        AccountSession.Rehydrate(
            entity.AccountId,
            entity.SessionId,
            entity.RefreshTokenHash,
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc,
            entity.RotatedAtUtc,
            entity.RevokedAtUtc);
}
