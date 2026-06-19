using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class EventTemplateEntityConfiguration : IEntityTypeConfiguration<EventTemplateEntity>
{
    public void Configure(EntityTypeBuilder<EventTemplateEntity> builder)
    {
        builder.ToTable("catalog_event_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.DefaultOutcomeKind).HasColumnName("default_outcome_kind").HasMaxLength(64).IsRequired();
        builder.Property(e => e.MinRiskLevel).HasColumnName("min_risk_level");
        builder.Property(e => e.MaxRiskLevel).HasColumnName("max_risk_level");
        builder.Property(e => e.RequiresPlayerChoice).HasColumnName("requires_player_choice");
        builder.Property(e => e.NarrativeTagsJson).HasColumnName("narrative_tags_json");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
