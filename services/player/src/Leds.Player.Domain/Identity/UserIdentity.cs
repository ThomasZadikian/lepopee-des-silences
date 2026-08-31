using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public sealed class UserIdentity
{
    private readonly HashSet<string> _recoveryCodeHashes;

    private UserIdentity(
        Guid id,
        Guid accountId,
        EmailAddress email,
        string passwordHash,
        AccountRole role,
        DateTimeOffset createdAtUtc,
        bool isEmailVerified = false,
        DateTimeOffset? emailVerifiedAtUtc = null,
        string? mfaSecretProtected = null,
        DateTimeOffset? mfaConfiguredAtUtc = null,
        IReadOnlyCollection<string>? recoveryCodeHashes = null)
    {
        Id = id;
        AccountId = accountId;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = createdAtUtc;
        IsEmailVerified = isEmailVerified;
        EmailVerifiedAtUtc = emailVerifiedAtUtc;
        MfaSecretProtected = mfaSecretProtected;
        MfaConfiguredAtUtc = mfaConfiguredAtUtc;
        IsMfaConfigured = !string.IsNullOrWhiteSpace(mfaSecretProtected) && mfaConfiguredAtUtc.HasValue;
        _recoveryCodeHashes = new HashSet<string>(
            recoveryCodeHashes ?? [],
            StringComparer.Ordinal);
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public AccountRole Role { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsEmailVerified { get; private set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }
    public bool IsMfaConfigured { get; private set; }
    public DateTimeOffset? MfaConfiguredAtUtc { get; private set; }
    public IReadOnlyCollection<string> RecoveryCodeHashes => _recoveryCodeHashes.ToArray();

    /// <summary>
    /// Encrypted/protected TOTP material. This value is safe to persist but must never be
    /// returned by an HTTP contract or logged. Only the authentication infrastructure may
    /// unprotect it when validating a TOTP code.
    /// </summary>
    public string? MfaSecretProtected { get; private set; }

    public static UserIdentity Register(
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        var id = Guid.NewGuid();
        return RegisterCore(id, id, email, passwordHash, createdAtUtc);
    }

    public static UserIdentity RegisterForAccount(
        Guid accountId,
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Account id is required.");

        return RegisterCore(Guid.NewGuid(), accountId, email, passwordHash, createdAtUtc);
    }

    private static UserIdentity RegisterCore(
        Guid id,
        Guid accountId,
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new UserIdentity(
            id,
            accountId,
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

    public void StageMfaSecret(string protectedTotpSecret)
    {
        if (!IsEmailVerified)
            throw new DomainException("Email must be verified before MFA can be configured.");
        if (IsMfaConfigured)
            throw new DomainException("MFA is already configured.");
        if (string.IsNullOrWhiteSpace(protectedTotpSecret))
            throw new DomainException("Protected TOTP secret is required.");

        MfaSecretProtected = protectedTotpSecret.Trim();
    }

    public void ConfigureMfa(string protectedTotpSecret, DateTimeOffset configuredAtUtc)
    {
        StageMfaSecret(protectedTotpSecret);
        IsMfaConfigured = true;
        MfaConfiguredAtUtc = configuredAtUtc;
    }

    public void ConfigureMfa(
        string protectedTotpSecret,
        DateTimeOffset configuredAtUtc,
        IReadOnlyCollection<string> recoveryCodeHashes)
    {
        ArgumentNullException.ThrowIfNull(recoveryCodeHashes);
        if (recoveryCodeHashes.Count == 0 || recoveryCodeHashes.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("At least one recovery-code hash is required when MFA is configured.");

        ConfigureMfa(protectedTotpSecret, configuredAtUtc);
        _recoveryCodeHashes.Clear();
        foreach (var hash in recoveryCodeHashes)
            _recoveryCodeHashes.Add(hash.Trim());
    }

    public bool TryConsumeRecoveryCodeHash(string recoveryCodeHash)
    {
        if (!IsMfaConfigured || string.IsNullOrWhiteSpace(recoveryCodeHash))
            return false;

        return _recoveryCodeHashes.Remove(recoveryCodeHash.Trim());
    }

    public void ChangeEmail(EmailAddress newEmail, DateTimeOffset changedAtUtc)
    {
        Email = newEmail;
        IsEmailVerified = false;
        EmailVerifiedAtUtc = null;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        PasswordHash = passwordHash.Trim();
    }

    public void AssignRole(AccountRole role)
    {
        if (!Enum.IsDefined(role))
            throw new DomainException("Account role is invalid.");

        Role = role;
    }

    public static UserIdentity Rehydrate(
        Guid id,
        Guid accountId,
        EmailAddress email,
        string passwordHash,
        AccountRole role,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? emailVerifiedAtUtc,
        string? mfaSecretProtected,
        DateTimeOffset? mfaConfiguredAtUtc,
        IReadOnlyCollection<string>? recoveryCodeHashes = null)
    {
        if (id == Guid.Empty || accountId == Guid.Empty)
            throw new DomainException("Identity and account ids are required.");

        return new UserIdentity(
            id,
            accountId,
            email,
            passwordHash,
            role,
            createdAtUtc,
            emailVerifiedAtUtc.HasValue,
            emailVerifiedAtUtc,
            mfaSecretProtected,
            mfaConfiguredAtUtc,
            recoveryCodeHashes);
    }
}
