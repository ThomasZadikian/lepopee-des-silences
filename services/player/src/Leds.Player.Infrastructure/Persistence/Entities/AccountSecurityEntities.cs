namespace Leds.Player.Infrastructure.Persistence.Entities;

public sealed class AccountIdentityEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Role { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; set; }
    public string? MfaSecretProtected { get; set; }
    public DateTimeOffset? MfaConfiguredAtUtc { get; set; }
    public string RecoveryCodeHashesJson { get; set; } = "[]";
    public DateTimeOffset? ClosureRequestedAtUtc { get; set; }
    public DateTimeOffset? ClosureExecuteAfterUtc { get; set; }
    public DateTimeOffset? ClosureCancelledAtUtc { get; set; }
}

public sealed class AccountSessionEntity
{
    public Guid SessionId { get; set; }
    public Guid AccountId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RotatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class SecurityTokenEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
}

public sealed class PrivacyConsentEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string PurposeKey { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class ActiveGameSessionLeaseEntity
{
    public Guid AccountId { get; set; }
    public Guid OwnerSessionId { get; set; }
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
