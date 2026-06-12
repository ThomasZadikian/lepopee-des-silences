using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunModifierEntityConfiguration : IEntityTypeConfiguration<RunModifierEntity>
{
    public void Configure(EntityTypeBuilder<RunModifierEntity> builder)
    {
        builder.ToTable("run_modifiers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.RunId).HasColumnName("run_id");
        builder.Property(m => m.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(m => m.Value).HasColumnName("value");
        builder.Property(m => m.Duration).HasColumnName("duration").HasMaxLength(64).IsRequired();
        builder.Property(m => m.SourceType).HasColumnName("source_type").HasMaxLength(64).IsRequired();
        builder.Property(m => m.SourceKey).HasColumnName("source_key").HasMaxLength(256).IsRequired();
        builder.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(m => m.ConsumedAtUtc).HasColumnName("consumed_at_utc");

        builder.HasOne(m => m.Run)
            .WithMany(run => run.RunModifiers)
            .HasForeignKey(m => m.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.RunId);
    }
}
