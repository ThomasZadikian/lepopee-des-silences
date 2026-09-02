using Leds.Player.Domain.Identity;

namespace Leds.Player.Application.Abstractions;

public sealed record OpaqueToken(string Value, string Hash);
public sealed record MfaEnrollment(string ProtectedSecret, string OtpAuthUri, string ManualEntryKey);
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

public interface IAuthenticationSecurity
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    OpaqueToken GenerateOpaqueToken();
    string HashOpaqueToken(string rawToken);
    MfaEnrollment CreateMfaEnrollment(EmailAddress email);
    bool VerifyTotp(string protectedSecret, string code, DateTimeOffset now);
}

public interface IAccessTokenIssuer
{
    AccessTokenResult Issue(
        UserIdentity identity,
        Guid sessionId,
        DateTimeOffset now,
        TimeSpan lifetime);
}

public interface IAccountEmailSender
{
    Task SendVerificationEmailAsync(
        EmailAddress recipient,
        string rawToken,
        CancellationToken cancellationToken);

    Task SendPasswordResetEmailAsync(
        EmailAddress recipient,
        string rawToken,
        CancellationToken cancellationToken);
}
