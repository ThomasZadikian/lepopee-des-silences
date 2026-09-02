using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public sealed class UserIdentity
{
    private readonly HashSet<string> _recoveryCodeHashes;

    private UserIdentity(UserIdentitySnapshot snapshot)
    {
        Id = snapshot.Id;
        AccountId = snapshot.AccountId;
        Email = snapshot.Email;
        PasswordHash = snapshot.PasswordHash;
        Role = snapshot.Role;
        CreatedAtUtc = snapshot.CreatedAtUtc;
        IsEmailVerified = snapshot.EmailVerifiedAtUtc.HasValue;
        EmailVerifiedAtUtc = snapshot.EmailVerifiedAtUtc;
        MfaSecretProtected = snapshot.MfaSecretProtected;
        MfaConfiguredAtUtc = snapshot.MfaConfiguredAtUtc;
        IsMfaConfigured = !string.IsNullOrWhiteSpace(snapshot.MfaSecretProtected)
            && snapshot.MfaConfiguredAtUtc.HasValue;
        _recoveryCodeHashes = new HashSet<string>(
            snapshot.RecoveryCodeHashes ?? [],
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
    public IReadOnlySet<string> RecoveryCodeHashes => _recoveryCodeHashes;

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

        return new UserIdentity(new UserIdentitySnapshot
        {
            Id = id,
            AccountId = accountId,
            Email = email,
            PasswordHash = passwordHash.Trim(),
            Role = AccountRole.Player,
            CreatedAtUtc = createdAtUtc
        });
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

    public static UserIdentity Rehydrate(UserIdentitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Id == Guid.Empty || snapshot.AccountId == Guid.Empty)
            throw new DomainException("Identity and account ids are required.");

        return new UserIdentity(snapshot);
    }
}

public sealed record UserIdentitySnapshot
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required EmailAddress Email { get; init; }
    public required string PasswordHash { get; init; }
    public required AccountRole Role { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; init; }
    public string? MfaSecretProtected { get; init; }
    public DateTimeOffset? MfaConfiguredAtUtc { get; init; }
    public IReadOnlyCollection<string>? RecoveryCodeHashes { get; init; }
}
