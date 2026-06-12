using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunItemEntityConfiguration : IEntityTypeConfiguration<RunItemEntity>
{
    public void Configure(EntityTypeBuilder<RunItemEntity> builder)
    {
        builder.ToTable("run_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.RunId).HasColumnName("run_id");
        builder.Property(item => item.DefinitionKey).HasColumnName("definition_key").HasMaxLength(256).IsRequired();
        builder.Property(item => item.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(1024);
        builder.Property(item => item.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(item => item.Rarity).HasColumnName("rarity").HasMaxLength(64).IsRequired();
        builder.Property(item => item.Quantity).HasColumnName("quantity");
        builder.Property(item => item.EffectType).HasColumnName("effect_type").HasMaxLength(64).IsRequired();
        builder.Property(item => item.EffectAmount).HasColumnName("effect_amount");
        builder.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasOne(item => item.Run)
            .WithMany(run => run.InventoryItems)
            .HasForeignKey(item => item.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.RunId);
    }
}
