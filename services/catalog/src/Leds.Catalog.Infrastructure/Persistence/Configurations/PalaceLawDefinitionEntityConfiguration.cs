using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class PalaceLawDefinitionEntityConfiguration : IEntityTypeConfiguration<PalaceLawDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<PalaceLawDefinitionEntity> builder)
    {
        builder.ToTable("catalog_palace_law_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.NarrativeText).HasColumnName("narrative_text");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(64).HasDefaultValue("Run").IsRequired();
        builder.Property(e => e.Duration).HasColumnName("duration").HasMaxLength(64).HasDefaultValue("UntilRunEnds").IsRequired();
        builder.Property(e => e.Trigger).HasColumnName("trigger").HasMaxLength(64);
        builder.Property(e => e.Severity).HasColumnName("severity").HasDefaultValue(1);
        builder.Property(e => e.Visibility).HasColumnName("visibility").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority");
        builder.Property(e => e.EffectSetId).HasColumnName("effect_set_id");
        builder.Property(e => e.BaseWeight).HasColumnName("base_weight").HasDefaultValue(1);
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.SelectionGroup).HasColumnName("selection_group").HasMaxLength(64);
        builder.Property(e => e.ImpactDomainsJson).HasColumnName("impact_domains_json").HasComment("Legacy JSON compatibility column. Structured effects/tags are relational in data-model-0.1.");
        builder.Property(e => e.Rarity).HasColumnName("rarity").HasMaxLength(32).HasDefaultValue("Commun").IsRequired();
        builder.Property(e => e.Polarity).HasColumnName("polarity").HasMaxLength(32).HasDefaultValue("Neutre").IsRequired();
        builder.Property(e => e.IsMajeure).HasColumnName("is_majeure").HasDefaultValue(false);
        builder.Property(e => e.RoomKey).HasColumnName("room_key").HasMaxLength(160);
        builder.Property(e => e.IsCumulExempt).HasColumnName("is_cumul_exempt").HasDefaultValue(false);
        builder.Property(e => e.ExclusionKeysJson).HasColumnName("exclusion_keys_json").HasDefaultValue("[]");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasOne(e => e.EffectSet).WithMany().HasForeignKey(e => e.EffectSetId).OnDelete(DeleteBehavior.SetNull);
    }
}
