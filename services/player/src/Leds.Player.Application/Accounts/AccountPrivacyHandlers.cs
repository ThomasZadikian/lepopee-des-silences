using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Privacy;
using MediatR;

namespace Leds.Player.Application.Accounts;

internal static class AccountManagementProjection
{
    public static AccountOverviewResponse Overview(UserIdentity identity, PlayerProfile profile)
    {
        var characters = profile.Roster.Characters
            .Where(character => !character.IsArchived && character.ArchetypeKey is not null)
            .Select(PlayerCharacterDto.FromDomain)
            .ToArray();

        return new AccountOverviewResponse(
            identity.AccountId,
            profile.DisplayName,
            identity.Email.Value,
            identity.Role.ToString(),
            identity.IsEmailVerified,
            identity.IsMfaConfigured,
            characters,
            MainStoryProgressDto.FromDomain(profile.MainStoryProgress));
    }

    public static PrivacyConsentResponse Consent(PrivacyConsent consent) => new(
        consent.PurposeKey,
        consent.PolicyVersion,
        consent.GrantedAtUtc,
        consent.RevokedAtUtc,
        consent.IsGranted);

    public static AccountClosureResponse Closure(AccountClosureRequest closure) => new(
        closure.RequestedAtUtc,
        closure.ExecuteAfterUtc,
        closure.CancelledAtUtc,
        closure.IsCancelled);
}

public sealed class UpdateAccountProfileCommandHandler
    : IRequestHandler<UpdateAccountProfileCommand, AccountOverviewResponse>
{
    private readonly IAccountProfileMaintenance _maintenance;
    private readonly IAccountStore _store;
    private readonly IPlayerProfileRepository _profiles;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public UpdateAccountProfileCommandHandler(
        IAccountProfileMaintenance maintenance,
        IAccountStore store,
        IPlayerProfileRepository profiles,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _maintenance = maintenance;
        _store = store;
        _profiles = profiles;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<AccountOverviewResponse> Handle(
        UpdateAccountProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new DomainException("Player display name is required.");

        var now = _timeProvider.GetUtcNow();
        await _maintenance.RenameAsync(
            new PlayerId(request.AccountId),
            request.DisplayName,
            now,
            cancellationToken);

        var identity = await _store.FindIdentityByAccountIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", request.AccountId);
        var profile = await _profiles.GetByIdAsync(new PlayerId(request.AccountId), cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);
        await _audit.WriteAsync(request.AccountId, "account.profile.updated", now, cancellationToken);
        return AccountManagementProjection.Overview(identity, profile);
    }
}

public sealed class ChangeAccountEmailCommandHandler
    : IRequestHandler<ChangeAccountEmailCommand, AccountEmailChangeResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccountEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public ChangeAccountEmailCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccountEmailSender emailSender,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _security = security;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<AccountEmailChangeResponse> Handle(
        ChangeAccountEmailCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _store.FindIdentityByAccountIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", request.AccountId);
        var email = EmailAddress.Create(request.NewEmail);

        if (email == identity.Email)
            return new AccountEmailChangeResponse(identity.Email.Value, !identity.IsEmailVerified);
        if (await _store.EmailExistsAsync(email, cancellationToken))
            throw new ConflictException("An account already exists for this email address.");

        var now = _timeProvider.GetUtcNow();
        identity.ChangeEmail(email, now);
        await _store.SaveIdentityAsync(identity, cancellationToken);

        var verification = _security.GenerateOpaqueToken();
        await _store.StoreSecurityTokenAsync(
            identity.AccountId,
            AccountSecurityPurposes.EmailVerification,
            verification.Hash,
            now,
            now.Add(AccountAuthenticationLifetimes.EmailVerification),
            cancellationToken);
        await _emailSender.SendVerificationEmailAsync(email, verification.Value, cancellationToken);
        await _audit.WriteAsync(identity.AccountId, "account.email.changed", now, cancellationToken);

        return new AccountEmailChangeResponse(email.Value, true);
    }
}

public sealed class ChangeAccountPasswordCommandHandler : IRequestHandler<ChangeAccountPasswordCommand>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly ICompromisedPasswordChecker _compromisedPasswords;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public ChangeAccountPasswordCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        ICompromisedPasswordChecker compromisedPasswords,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _security = security;
        _compromisedPasswords = compromisedPasswords;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task Handle(ChangeAccountPasswordCommand request, CancellationToken cancellationToken)
    {
        var identity = await _store.FindIdentityByAccountIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", request.AccountId);
        if (!_security.VerifyPassword(request.CurrentPassword, identity.PasswordHash))
            throw new UnauthorizedException("Current password is invalid.");

        PasswordPolicy.EnsureAcceptable(request.NewPassword);
        if (await _compromisedPasswords.IsCompromisedAsync(request.NewPassword, cancellationToken))
            throw new DomainException("This password appears in known data breaches and cannot be used.");

        var now = _timeProvider.GetUtcNow();
        identity.ChangePasswordHash(_security.HashPassword(request.NewPassword));
        await _store.SaveIdentityAsync(identity, cancellationToken);
        await _store.RevokeSessionsAsync(identity.AccountId, now, cancellationToken);
        await _audit.WriteAsync(identity.AccountId, "account.password.changed", now, cancellationToken);
    }
}

public sealed class GetPrivacyStateQueryHandler : IRequestHandler<GetPrivacyStateQuery, AccountPrivacyResponse>
{
    private readonly IAccountStore _store;

    public GetPrivacyStateQueryHandler(IAccountStore store) => _store = store;

    public async Task<AccountPrivacyResponse> Handle(
        GetPrivacyStateQuery request,
        CancellationToken cancellationToken)
    {
        var consents = await _store.ListConsentsAsync(request.AccountId, cancellationToken);
        var closure = await _store.GetClosureRequestAsync(request.AccountId, cancellationToken);
        return new AccountPrivacyResponse(
            consents.Select(AccountManagementProjection.Consent).ToArray(),
            closure is null ? null : AccountManagementProjection.Closure(closure));
    }
}

public sealed class SetPrivacyConsentCommandHandler
    : IRequestHandler<SetPrivacyConsentCommand, PrivacyConsentResponse>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public SetPrivacyConsentCommandHandler(
        IAccountStore store,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<PrivacyConsentResponse> Handle(
        SetPrivacyConsentCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var existing = (await _store.ListConsentsAsync(request.AccountId, cancellationToken))
            .Where(consent => string.Equals(consent.PurposeKey, request.PurposeKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(consent.PolicyVersion, request.PolicyVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(consent => consent.GrantedAtUtc)
            .FirstOrDefault();

        PrivacyConsent target;
        if (request.Granted)
        {
            target = existing?.IsGranted == true
                ? existing
                : PrivacyConsent.Grant(request.PurposeKey, request.PolicyVersion, now);
        }
        else
        {
            if (existing is null)
                throw new NotFoundException("Privacy consent", request.AccountId);
            target = existing;
            target.Revoke(now);
        }

        await _store.SaveConsentAsync(request.AccountId, target, cancellationToken);
        await _audit.WriteAsync(
            request.AccountId,
            request.Granted ? "privacy.consent.granted" : "privacy.consent.revoked",
            now,
            cancellationToken);
        return AccountManagementProjection.Consent(target);
    }
}

public sealed class RequestAccountClosureCommandHandler
    : IRequestHandler<RequestAccountClosureCommand, AccountClosureResponse>
{
    internal static readonly TimeSpan GracePeriod = TimeSpan.FromDays(30);

    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public RequestAccountClosureCommandHandler(
        IAccountStore store,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<AccountClosureResponse> Handle(
        RequestAccountClosureCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetClosureRequestAsync(request.AccountId, cancellationToken);
        if (existing is not null && !existing.IsCancelled)
            return AccountManagementProjection.Closure(existing);

        var now = _timeProvider.GetUtcNow();
        var closure = AccountClosureRequest.Request(request.AccountId, now, GracePeriod);
        await _store.SaveClosureRequestAsync(closure, cancellationToken);
        await _audit.WriteAsync(request.AccountId, "privacy.closure.requested", now, cancellationToken);
        return AccountManagementProjection.Closure(closure);
    }
}

public sealed class CancelAccountClosureCommandHandler
    : IRequestHandler<CancelAccountClosureCommand, AccountClosureResponse>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public CancelAccountClosureCommandHandler(
        IAccountStore store,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<AccountClosureResponse> Handle(
        CancelAccountClosureCommand request,
        CancellationToken cancellationToken)
    {
        var closure = await _store.GetClosureRequestAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account closure", request.AccountId);
        var now = _timeProvider.GetUtcNow();
        closure.Cancel(now);
        await _store.SaveClosureRequestAsync(closure, cancellationToken);
        await _audit.WriteAsync(request.AccountId, "privacy.closure.cancelled", now, cancellationToken);
        return AccountManagementProjection.Closure(closure);
    }
}

public sealed class GetAccountDataExportQueryHandler
    : IRequestHandler<GetAccountDataExportQuery, AccountDataExportResponse>
{
    private readonly IAccountStore _store;
    private readonly IPlayerProfileRepository _profiles;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public GetAccountDataExportQueryHandler(
        IAccountStore store,
        IPlayerProfileRepository profiles,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _store = store;
        _profiles = profiles;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<AccountDataExportResponse> Handle(
        GetAccountDataExportQuery request,
        CancellationToken cancellationToken)
    {
        var identity = await _store.FindIdentityByAccountIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", request.AccountId);
        var profile = await _profiles.GetByIdAsync(new PlayerId(request.AccountId), cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);
        var consents = await _store.ListConsentsAsync(request.AccountId, cancellationToken);
        var closure = await _store.GetClosureRequestAsync(request.AccountId, cancellationToken);
        var sessions = await _store.ListSessionsAsync(request.AccountId, cancellationToken);
        var now = _timeProvider.GetUtcNow();

        var response = new AccountDataExportResponse(
            "LEDS-account-export-v1",
            now,
            new AccountExportIdentity(
                request.AccountId,
                profile.DisplayName,
                identity.Email.Value,
                identity.Role.ToString(),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc,
                identity.CreatedAtUtc,
                identity.IsEmailVerified,
                identity.IsMfaConfigured),
            profile.Roster.Characters.Select(PlayerCharacterDto.FromDomain).ToArray(),
            MainStoryProgressDto.FromDomain(profile.MainStoryProgress),
            consents.Select(AccountManagementProjection.Consent).ToArray(),
            closure is null ? null : AccountManagementProjection.Closure(closure),
            sessions.Select(session => new AccountSessionResponse(
                session.SessionId,
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.RotatedAtUtc,
                session.RevokedAtUtc,
                false,
                !session.IsRevoked && !session.IsExpired(now))).ToArray());

        await _audit.WriteAsync(request.AccountId, "privacy.export.generated", now, cancellationToken);
        return response;
    }
}

public sealed class ExecuteDueAccountClosuresCommandHandler
    : IRequestHandler<ExecuteDueAccountClosuresCommand, int>
{
    private readonly IAccountPrivacyMaintenanceStore _maintenance;
    private readonly IAccountProfileMaintenance _profiles;
    private readonly TimeProvider _timeProvider;
    private readonly IAccountAuditLog _audit;

    public ExecuteDueAccountClosuresCommandHandler(
        IAccountPrivacyMaintenanceStore maintenance,
        IAccountProfileMaintenance profiles,
        TimeProvider timeProvider,
        IAccountAuditLog? audit = null)
    {
        _maintenance = maintenance;
        _profiles = profiles;
        _timeProvider = timeProvider;
        _audit = audit ?? new NullAccountAuditLog();
    }

    public async Task<int> Handle(
        ExecuteDueAccountClosuresCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var accounts = await _maintenance.ListExecutableClosureAccountIdsAsync(now, cancellationToken);
        var processed = 0;
        foreach (var accountId in accounts)
        {
            await _profiles.AnonymizeAsync(new PlayerId(accountId), now, cancellationToken);
            await _maintenance.PurgeAuthenticationMaterialAsync(accountId, now, cancellationToken);
            await _audit.WriteAsync(accountId, "privacy.account.anonymised", now, cancellationToken);
            processed++;
        }

        return processed;
    }
}
