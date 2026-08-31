using MediatR;

namespace Leds.Player.Application.Accounts;

public sealed record RegisterAccountCommand(
    string DisplayName,
    string Email,
    string Password,
    bool AgeConfirmed) : IRequest<RegisterAccountResponse>;

public sealed record RegisterAccountResponse(
    Guid AccountId,
    string Email,
    bool EmailVerificationRequired);

public sealed record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResponse>;
public sealed record VerifyEmailResponse(Guid AccountId, bool Verified);

public sealed record BeginLoginCommand(string Email, string Password) : IRequest<BeginLoginResponse>;
public sealed record BeginLoginResponse(
    string Status,
    string? ChallengeToken = null,
    bool EmailVerificationRequired = false);

public sealed record BeginMfaEnrollmentCommand(string ChallengeToken) : IRequest<MfaEnrollmentResponse>;
public sealed record MfaEnrollmentResponse(
    string ChallengeToken,
    string ProtectedSecret,
    string OtpAuthUri,
    string ManualEntryKey);

public sealed record ConfirmMfaEnrollmentCommand(
    string ChallengeToken,
    string ProtectedSecret,
    string Code) : IRequest<AuthenticatedSessionResponse>;

public sealed record CompleteMfaChallengeCommand(
    string ChallengeToken,
    string Code) : IRequest<AuthenticatedSessionResponse>;

public sealed record RefreshSessionCommand(
    Guid SessionId,
    string RefreshToken) : IRequest<AuthenticatedSessionResponse>;

public sealed record LogoutSessionCommand(Guid AccountId, Guid SessionId) : IRequest;

public sealed record RequestPasswordResetCommand(string Email) : IRequest;
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed record AuthenticatedSessionResponse(
    Guid AccountId,
    Guid SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
