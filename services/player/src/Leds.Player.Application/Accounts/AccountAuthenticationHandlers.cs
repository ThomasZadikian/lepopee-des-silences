using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Common;
using Leds.Player.Domain.Identity;
using Leds.Player.Domain.Players;
using Leds.Player.Domain.Sessions;
using MediatR;

namespace Leds.Player.Application.Accounts;

internal static class AccountAuthenticationLifetimes
{
    public static readonly TimeSpan EmailVerification = TimeSpan.FromHours(24);
    public static readonly TimeSpan MfaSetupChallenge = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MfaChallenge = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan PasswordReset = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan AccessToken = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshSession = TimeSpan.FromDays(30);
}

internal static class AccountSecurityPurposes
{
    public const string EmailVerification = "email-verification";
    public const string MfaSetup = "mfa-setup";
    public const string MfaChallenge = "mfa-challenge";
    public const string PasswordReset = "password-reset";
}

public sealed class RegisterAccountCommandHandler
    : IRequestHandler<RegisterAccountCommand, RegisterAccountResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccountEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public RegisterAccountCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccountEmailSender emailSender,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterAccountResponse> Handle(
        RegisterAccountCommand request,
        CancellationToken cancellationToken)
    {
        MinimumAgePolicy.EnsureEligible(request.AgeConfirmed);
        PasswordPolicy.EnsureAcceptable(request.Password);
        var email = EmailAddress.Create(request.Email);

        if (await _store.EmailExistsAsync(email, cancellationToken))
            throw new ConflictException("An account already exists for this email address.");

        var now = _timeProvider.GetUtcNow();
        var profile = PlayerProfile.Create(request.DisplayName, now);
        var identity = UserIdentity.RegisterForAccount(
            profile.Id.Value,
            email,
            _security.HashPassword(request.Password),
            now);
        var verificationToken = _security.GenerateOpaqueToken();

        await _store.RegisterAsync(
            profile,
            identity,
            verificationToken.Hash,
            now.Add(AccountAuthenticationLifetimes.EmailVerification),
            cancellationToken);
        await _emailSender.SendVerificationEmailAsync(email, verificationToken.Value, cancellationToken);

        return new RegisterAccountResponse(profile.Id.Value, email.Value, true);
    }
}

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly TimeProvider _timeProvider;

    public VerifyEmailCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _timeProvider = timeProvider;
    }

    public async Task<VerifyEmailResponse> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var tokenHash = _security.HashOpaqueToken(request.Token);
        var token = await _store.FindSecurityTokenAsync(
            AccountSecurityPurposes.EmailVerification,
            tokenHash,
            cancellationToken);

        if (token is null || !token.CanConsume(now))
            throw new DomainException("Email verification token is invalid or expired.");

        var identity = await _store.FindIdentityByAccountIdAsync(token.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", token.AccountId);
        identity.VerifyEmail(now);
        await _store.SaveIdentityAsync(identity, cancellationToken);
        await _store.ConsumeSecurityTokenAsync(token.Id, now, cancellationToken);

        return new VerifyEmailResponse(identity.AccountId, true);
    }
}

public sealed class BeginLoginCommandHandler : IRequestHandler<BeginLoginCommand, BeginLoginResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly TimeProvider _timeProvider;

    public BeginLoginCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _timeProvider = timeProvider;
    }

    public async Task<BeginLoginResponse> Handle(
        BeginLoginCommand request,
        CancellationToken cancellationToken)
    {
        EmailAddress email;
        try
        {
            email = EmailAddress.Create(request.Email);
        }
        catch (DomainException)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var identity = await _store.FindIdentityByEmailAsync(email, cancellationToken);
        if (identity is null || !_security.VerifyPassword(request.Password, identity.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!identity.IsEmailVerified)
            return new BeginLoginResponse("email-verification-required", EmailVerificationRequired: true);

        var now = _timeProvider.GetUtcNow();
        var challenge = _security.GenerateOpaqueToken();
        var purpose = identity.IsMfaConfigured
            ? AccountSecurityPurposes.MfaChallenge
            : AccountSecurityPurposes.MfaSetup;
        var lifetime = identity.IsMfaConfigured
            ? AccountAuthenticationLifetimes.MfaChallenge
            : AccountAuthenticationLifetimes.MfaSetupChallenge;

        await _store.StoreSecurityTokenAsync(
            identity.AccountId,
            purpose,
            challenge.Hash,
            now,
            now.Add(lifetime),
            cancellationToken);

        return new BeginLoginResponse(
            identity.IsMfaConfigured ? "mfa-required" : "mfa-setup-required",
            challenge.Value);
    }
}

public sealed class BeginMfaEnrollmentCommandHandler
    : IRequestHandler<BeginMfaEnrollmentCommand, MfaEnrollmentResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly TimeProvider _timeProvider;

    public BeginMfaEnrollmentCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _timeProvider = timeProvider;
    }

    public async Task<MfaEnrollmentResponse> Handle(
        BeginMfaEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var token = await GetValidChallengeAsync(
            _store,
            _security,
            request.ChallengeToken,
            AccountSecurityPurposes.MfaSetup,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        var identity = await _store.FindIdentityByAccountIdAsync(token.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", token.AccountId);
        if (!identity.IsEmailVerified)
            throw new UnauthorizedException("Email verification is required before MFA setup.");
        if (identity.IsMfaConfigured)
            throw new ConflictException("MFA is already configured for this account.");

        var enrollment = _security.CreateMfaEnrollment(identity.Email);
        return new MfaEnrollmentResponse(
            request.ChallengeToken,
            enrollment.ProtectedSecret,
            enrollment.OtpAuthUri,
            enrollment.ManualEntryKey);
    }

    internal static async Task<SecurityTokenState> GetValidChallengeAsync(
        IAccountStore store,
        IAuthenticationSecurity security,
        string rawToken,
        string purpose,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var token = await store.FindSecurityTokenAsync(
            purpose,
            security.HashOpaqueToken(rawToken),
            cancellationToken);
        if (token is null || !token.CanConsume(now))
            throw new UnauthorizedException("Authentication challenge is invalid or expired.");
        return token;
    }
}

public sealed class ConfirmMfaEnrollmentCommandHandler
    : IRequestHandler<ConfirmMfaEnrollmentCommand, AuthenticatedSessionResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly TimeProvider _timeProvider;

    public ConfirmMfaEnrollmentCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccessTokenIssuer accessTokenIssuer,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _accessTokenIssuer = accessTokenIssuer;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticatedSessionResponse> Handle(
        ConfirmMfaEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var challenge = await BeginMfaEnrollmentCommandHandler.GetValidChallengeAsync(
            _store,
            _security,
            request.ChallengeToken,
            AccountSecurityPurposes.MfaSetup,
            now,
            cancellationToken);
        var identity = await _store.FindIdentityByAccountIdAsync(challenge.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", challenge.AccountId);

        if (!_security.VerifyTotp(request.ProtectedSecret, request.Code, now))
            throw new UnauthorizedException("Invalid authentication code.");

        identity.ConfigureMfa(request.ProtectedSecret, now);
        await _store.SaveIdentityAsync(identity, cancellationToken);
        await _store.ConsumeSecurityTokenAsync(challenge.Id, now, cancellationToken);

        return await CreateSessionAsync(
            _store,
            _security,
            _accessTokenIssuer,
            identity,
            now,
            cancellationToken);
    }

    internal static async Task<AuthenticatedSessionResponse> CreateSessionAsync(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccessTokenIssuer accessTokenIssuer,
        UserIdentity identity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!AuthenticationPolicy.CanStartInteractiveSession(identity))
            throw new UnauthorizedException("Interactive authentication requirements are not satisfied.");

        var refresh = security.GenerateOpaqueToken();
        var sessionId = Guid.NewGuid();
        var refreshExpires = now.Add(AccountAuthenticationLifetimes.RefreshSession);
        var session = AccountSession.Create(
            identity.AccountId,
            sessionId,
            refresh.Hash,
            now,
            refreshExpires);
        await store.AddSessionAsync(session, cancellationToken);

        var access = accessTokenIssuer.Issue(
            identity,
            sessionId,
            now,
            AccountAuthenticationLifetimes.AccessToken);
        return new AuthenticatedSessionResponse(
            identity.AccountId,
            sessionId,
            access.Token,
            access.ExpiresAtUtc,
            refresh.Value,
            refreshExpires);
    }
}

public sealed class CompleteMfaChallengeCommandHandler
    : IRequestHandler<CompleteMfaChallengeCommand, AuthenticatedSessionResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly TimeProvider _timeProvider;

    public CompleteMfaChallengeCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccessTokenIssuer accessTokenIssuer,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _accessTokenIssuer = accessTokenIssuer;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticatedSessionResponse> Handle(
        CompleteMfaChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var challenge = await BeginMfaEnrollmentCommandHandler.GetValidChallengeAsync(
            _store,
            _security,
            request.ChallengeToken,
            AccountSecurityPurposes.MfaChallenge,
            now,
            cancellationToken);
        var identity = await _store.FindIdentityByAccountIdAsync(challenge.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", challenge.AccountId);

        if (identity.MfaSecretProtected is null
            || !_security.VerifyTotp(identity.MfaSecretProtected, request.Code, now))
        {
            throw new UnauthorizedException("Invalid authentication code.");
        }

        await _store.ConsumeSecurityTokenAsync(challenge.Id, now, cancellationToken);
        return await ConfirmMfaEnrollmentCommandHandler.CreateSessionAsync(
            _store,
            _security,
            _accessTokenIssuer,
            identity,
            now,
            cancellationToken);
    }
}

public sealed class RefreshSessionCommandHandler
    : IRequestHandler<RefreshSessionCommand, AuthenticatedSessionResponse>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccessTokenIssuer accessTokenIssuer,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _accessTokenIssuer = accessTokenIssuer;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticatedSessionResponse> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var session = await _store.FindSessionAsync(request.SessionId, cancellationToken);
        var presentedHash = _security.HashOpaqueToken(request.RefreshToken);
        if (session is null || session.IsExpired(now) || !session.MatchesRefreshTokenHash(presentedHash))
            throw new UnauthorizedException("Refresh session is invalid or expired.");

        var identity = await _store.FindIdentityByAccountIdAsync(session.AccountId, cancellationToken)
            ?? throw new UnauthorizedException("Account identity no longer exists.");
        var refresh = _security.GenerateOpaqueToken();
        var refreshExpires = now.Add(AccountAuthenticationLifetimes.RefreshSession);
        session.RotateRefreshToken(refresh.Hash, refreshExpires, now);
        await _store.SaveSessionAsync(session, cancellationToken);

        var access = _accessTokenIssuer.Issue(
            identity,
            session.SessionId,
            now,
            AccountAuthenticationLifetimes.AccessToken);
        return new AuthenticatedSessionResponse(
            identity.AccountId,
            session.SessionId,
            access.Token,
            access.ExpiresAtUtc,
            refresh.Value,
            refreshExpires);
    }
}

public sealed class LogoutSessionCommandHandler : IRequestHandler<LogoutSessionCommand>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;

    public LogoutSessionCommandHandler(IAccountStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task Handle(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _store.FindSessionAsync(request.SessionId, cancellationToken);
        if (session is null || session.AccountId != request.AccountId)
            return;

        session.Revoke(_timeProvider.GetUtcNow());
        await _store.SaveSessionAsync(session, cancellationToken);
    }
}

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly IAccountEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public RequestPasswordResetCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        IAccountEmailSender emailSender,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        EmailAddress email;
        try
        {
            email = EmailAddress.Create(request.Email);
        }
        catch (DomainException)
        {
            return;
        }

        var identity = await _store.FindIdentityByEmailAsync(email, cancellationToken);
        if (identity is null)
            return;

        var now = _timeProvider.GetUtcNow();
        var token = _security.GenerateOpaqueToken();
        await _store.StoreSecurityTokenAsync(
            identity.AccountId,
            AccountSecurityPurposes.PasswordReset,
            token.Hash,
            now,
            now.Add(AccountAuthenticationLifetimes.PasswordReset),
            cancellationToken);
        await _emailSender.SendPasswordResetEmailAsync(identity.Email, token.Value, cancellationToken);
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAccountStore _store;
    private readonly IAuthenticationSecurity _security;
    private readonly TimeProvider _timeProvider;

    public ResetPasswordCommandHandler(
        IAccountStore store,
        IAuthenticationSecurity security,
        TimeProvider timeProvider)
    {
        _store = store;
        _security = security;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordPolicy.EnsureAcceptable(request.NewPassword);
        var now = _timeProvider.GetUtcNow();
        var token = await _store.FindSecurityTokenAsync(
            AccountSecurityPurposes.PasswordReset,
            _security.HashOpaqueToken(request.Token),
            cancellationToken);
        if (token is null || !token.CanConsume(now))
            throw new DomainException("Password-reset token is invalid or expired.");

        var identity = await _store.FindIdentityByAccountIdAsync(token.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", token.AccountId);
        identity.ChangePasswordHash(_security.HashPassword(request.NewPassword));
        await _store.SaveIdentityAsync(identity, cancellationToken);
        await _store.ConsumeSecurityTokenAsync(token.Id, now, cancellationToken);
        await _store.RevokeSessionsAsync(identity.AccountId, now, cancellationToken);
    }
}
