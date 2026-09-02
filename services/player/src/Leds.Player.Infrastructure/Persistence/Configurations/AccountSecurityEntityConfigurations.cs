using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Player.Infrastructure.Persistence.Configurations;

public sealed class AccountIdentityEntityConfiguration : IEntityTypeConfiguration<AccountIdentityEntity>
{
    public void Configure(EntityTypeBuilder<AccountIdentityEntity> builder)
    {
        builder.ToTable("account_identities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasDefaultValue(0);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.EmailVerifiedAtUtc).HasColumnName("email_verified_at_utc");
        builder.Property(x => x.MfaSecretProtected).HasColumnName("mfa_secret_protected").HasMaxLength(2048);
        builder.Property(x => x.MfaConfiguredAtUtc).HasColumnName("mfa_configured_at_utc");
        builder.Property(x => x.RecoveryCodeHashesJson).HasColumnName("recovery_code_hashes_json").HasDefaultValue("[]").IsRequired();
        builder.Property(x => x.ClosureRequestedAtUtc).HasColumnName("closure_requested_at_utc");
        builder.Property(x => x.ClosureExecuteAfterUtc).HasColumnName("closure_execute_after_utc");
        builder.Property(x => x.ClosureCancelledAtUtc).HasColumnName("closure_cancelled_at_utc");

        builder.HasIndex(x => x.AccountId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasOne<PlayerProfileEntity>()
            .WithOne()
            .HasForeignKey<AccountIdentityEntity>(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AccountSessionEntityConfiguration : IEntityTypeConfiguration<AccountSessionEntity>
{
    public void Configure(EntityTypeBuilder<AccountSessionEntity> builder)
    {
        builder.ToTable("account_sessions");
        builder.HasKey(x => x.SessionId);
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.RotatedAtUtc).HasColumnName("rotated_at_utc");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.HasIndex(x => x.AccountId);
        builder.HasOne<PlayerProfileEntity>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SecurityTokenEntityConfiguration : IEntityTypeConfiguration<SecurityTokenEntity>
{
    public void Configure(EntityTypeBuilder<SecurityTokenEntity> builder)
    {
        builder.ToTable("account_security_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IssuedAtUtc).HasColumnName("issued_at_utc");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        builder.HasIndex(x => new { x.Purpose, x.TokenHash }).IsUnique();
        builder.HasIndex(x => x.AccountId);
        builder.HasOne<PlayerProfileEntity>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrivacyConsentEntityConfiguration : IEntityTypeConfiguration<PrivacyConsentEntity>
{
    public void Configure(EntityTypeBuilder<PrivacyConsentEntity> builder)
    {
        builder.ToTable("account_privacy_consents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.PurposeKey).HasColumnName("purpose_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.PolicyVersion).HasColumnName("policy_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.GrantedAtUtc).HasColumnName("granted_at_utc");
        builder.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.HasIndex(x => new { x.AccountId, x.PurposeKey, x.GrantedAtUtc });
        builder.HasOne<PlayerProfileEntity>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ActiveGameSessionLeaseEntityConfiguration : IEntityTypeConfiguration<ActiveGameSessionLeaseEntity>
{
    public void Configure(EntityTypeBuilder<ActiveGameSessionLeaseEntity> builder)
    {
        builder.ToTable("active_game_session_leases");
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.OwnerSessionId).HasColumnName("owner_session_id");
        builder.Property(x => x.AcquiredAtUtc).HasColumnName("acquired_at_utc");
        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.HasOne<PlayerProfileEntity>()
            .WithOne()
            .HasForeignKey<ActiveGameSessionLeaseEntity>(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
