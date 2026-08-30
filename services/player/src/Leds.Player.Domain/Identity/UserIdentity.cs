using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public sealed class UserIdentity
{
    private UserIdentity(
        Guid id,
        EmailAddress email,
        string passwordHash,
        AccountRole role,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public AccountRole Role { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsEmailVerified { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public bool IsMfaConfigured { get; private set; }
    public DateTimeOffset? MfaConfiguredAtUtc { get; private set; }

    private string? MfaSecretProtected { get; set; }

    public static UserIdentity Register(
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new UserIdentity(
            Guid.NewGuid(),
            email,
            passwordHash.Trim(),
            AccountRole.Player,
            createdAtUtc);
    }

    public void VerifyEmail(DateTimeOffset verifiedAtUtc)
    {
        if (IsEmailVerified)
            return;

        IsEmailVerified = true;
        EmailVerifiedAtUtc = verifiedAtUtc;
    }

    public void ConfigureMfa(string protectedTotpSecret, DateTimeOffset configuredAtUtc)
    {
        if (!IsEmailVerified)
            throw new DomainException("Email must be verified before MFA can be configured.");

        if (string.IsNullOrWhiteSpace(protectedTotpSecret))
            throw new DomainException("Protected TOTP secret is required.");

        MfaSecretProtected = protectedTotpSecret.Trim();
        IsMfaConfigured = true;
        MfaConfiguredAtUtc = configuredAtUtc;
    }

    public void ChangeEmail(EmailAddress newEmail, DateTimeOffset changedAtUtc)
    {
        Email = newEmail;
        IsEmailVerified = false;
        EmailVerifiedAtUtc = null;
    }
}
