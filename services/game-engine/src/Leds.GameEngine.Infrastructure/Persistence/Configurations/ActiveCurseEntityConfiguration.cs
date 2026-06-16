using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class ActiveCurseEntityConfiguration : IEntityTypeConfiguration<Entities.ActiveCurseEntity>
{
    public void Configure(EntityTypeBuilder<Entities.ActiveCurseEntity> builder)
    {
        builder.ToTable("run_active_curses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.RunId)
            .HasColumnName("run_id")
            .IsRequired();

        builder.Property(e => e.Key)
            .HasColumnName("key")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1024)
            .HasDefaultValue("");

        builder.Property(e => e.DifficultyDelta)
            .HasColumnName("difficulty_delta")
            .HasDefaultValue(0.0);

        builder.Property(e => e.AppliedAtUtc)
            .HasColumnName("applied_at_utc")
            .IsRequired();

        builder.Property(e => e.CurseDefinitionKey)
            .HasColumnName("curse_definition_key")
            .HasMaxLength(128);

        builder.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasDefaultValue(3);

        builder.Property(e => e.Duration)
            .HasColumnName("duration")
            .HasMaxLength(64)
            .HasDefaultValue("NextCombatOnly");

        builder.Property(e => e.ExpiresAtRoomId)
            .HasColumnName("expires_at_room_id");

        builder.Property(e => e.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc");

        builder.Property(e => e.EffectSetKey)
            .HasColumnName("effect_set_key")
            .HasMaxLength(160);

        builder.HasOne(e => e.Run)
            .WithMany()
            .HasForeignKey(e => e.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RunId)
            .HasDatabaseName("IX_run_active_curses_run_id");

        builder.HasIndex(e => e.CurseDefinitionKey)
            .HasDatabaseName("IX_run_active_curses_curse_definition_key");
    }
}
