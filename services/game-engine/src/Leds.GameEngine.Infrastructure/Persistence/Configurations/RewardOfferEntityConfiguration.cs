using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RewardOfferEntityConfiguration : IEntityTypeConfiguration<RewardOfferEntity>
{
    public void Configure(EntityTypeBuilder<RewardOfferEntity> builder)
    {
        builder.ToTable("run_reward_offers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.RunId).HasColumnName("run_id").IsRequired();
        builder.Property(o => o.Source).HasColumnName("source").HasMaxLength(64).IsRequired();
        builder.Property(o => o.State).HasColumnName("state").HasMaxLength(32).IsRequired();
        builder.Property(o => o.CombatTier).HasColumnName("combat_tier").HasMaxLength(32);
        builder.Property(o => o.DifficultyMultiplier).HasColumnName("difficulty_multiplier").HasColumnType("decimal(10,4)");
        builder.Property(o => o.RewardPowerMultiplier).HasColumnName("reward_power_multiplier").HasColumnType("decimal(10,4)");
        builder.Property(o => o.CatalogRewardTemplateKey).HasColumnName("catalog_reward_template_key").HasMaxLength(160);
        builder.Property(o => o.CatalogRewardTemplateVersion).HasColumnName("catalog_reward_template_version").HasMaxLength(32);
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(o => o.SelectedAtUtc).HasColumnName("selected_at_utc");

        builder.HasMany(o => o.Options)
            .WithOne(opt => opt.RewardOffer)
            .HasForeignKey(opt => opt.RewardOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.RunId);
        builder.HasIndex(o => o.State);
    }
}

public sealed class RewardOptionEntityConfiguration : IEntityTypeConfiguration<RewardOptionEntity>
{
    public void Configure(EntityTypeBuilder<RewardOptionEntity> builder)
    {
        builder.ToTable("run_reward_options");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.RewardOfferId).HasColumnName("reward_offer_id").IsRequired();
        builder.Property(o => o.RewardType).HasColumnName("reward_type").HasMaxLength(64).IsRequired();
        builder.Property(o => o.Label).HasColumnName("label").HasMaxLength(256).IsRequired();
        builder.Property(o => o.Description).HasColumnName("description").IsRequired();
        builder.Property(o => o.PayloadKey).HasColumnName("payload_key").HasMaxLength(256);
        builder.Property(o => o.PayloadType).HasColumnName("payload_type").HasMaxLength(64);
        builder.Property(o => o.EffectSetKey).HasColumnName("effect_set_key").HasMaxLength(160);
        builder.Property(o => o.EffectSetVersion).HasColumnName("effect_set_version").HasMaxLength(32);
        builder.Property(o => o.BaseAmount).HasColumnName("base_amount");
        builder.Property(o => o.ScaledAmount).HasColumnName("scaled_amount");
        builder.Property(o => o.IsSelected).HasColumnName("is_selected").HasDefaultValue(false);
        builder.Property(o => o.SelectionOrder).HasColumnName("selection_order").HasDefaultValue(0);

        builder.HasOne(o => o.RewardOffer)
            .WithMany(offer => offer.Options)
            .HasForeignKey(o => o.RewardOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.RewardOfferId);
    }
}
