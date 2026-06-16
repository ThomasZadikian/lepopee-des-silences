using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunActivePalaceLawEntityConfiguration : IEntityTypeConfiguration<RunActivePalaceLawEntity>
{
    public void Configure(EntityTypeBuilder<RunActivePalaceLawEntity> builder)
    {
        builder.ToTable("run_active_palace_laws");

        builder.HasKey(law => law.Id);

        builder.Property(law => law.Id).HasColumnName("id");
        builder.Property(law => law.RunId).HasColumnName("run_id");
        builder.Property(law => law.LawId).HasColumnName("law_id");
        builder.Property(law => law.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        builder.Property(law => law.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(law => law.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(law => law.Domains).HasColumnName("domains").HasMaxLength(512).IsRequired();

        builder.Property(law => law.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);

        builder.Property(law => law.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(law => law.Duration)
            .HasColumnName("duration")
            .HasMaxLength(64);

        builder.Property(law => law.AppliedAtUtc)
            .HasColumnName("applied_at_utc");

        builder.Property(law => law.ExpiresAtRoomId)
            .HasColumnName("expires_at_room_id");

        builder.Property(law => law.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc");

        builder.HasOne(law => law.Run)
            .WithMany(run => run.ActivePalaceLaws)
            .HasForeignKey(law => law.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(law => law.RunId)
            .HasDatabaseName("IX_run_active_palace_laws_run_id");

        builder.HasIndex(law => law.Key)
            .HasDatabaseName("IX_run_active_palace_laws_key");
    }
}
