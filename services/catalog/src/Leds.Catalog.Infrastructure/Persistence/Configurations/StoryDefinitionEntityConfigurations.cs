using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class StorySequenceDefinitionEntityConfiguration
    : IEntityTypeConfiguration<StorySequenceDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<StorySequenceDefinitionEntity> builder)
    {
        builder.ToTable("story_sequence_definitions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id");
        builder.Property(entity => entity.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.EntryStepKey).HasColumnName("entry_step_key").HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(entity => entity.Key).IsUnique();
    }
}

public sealed class StoryStepDefinitionEntityConfiguration
    : IEntityTypeConfiguration<StoryStepDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<StoryStepDefinitionEntity> builder)
    {
        builder.ToTable("story_step_definitions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id");
        builder.Property(entity => entity.SequenceDefinitionId).HasColumnName("sequence_definition_id");
        builder.Property(entity => entity.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Order).HasColumnName("step_order");
        builder.Property(entity => entity.RoomDefinitionKey).HasColumnName("room_definition_key").HasMaxLength(160);
        builder.Property(entity => entity.ConditionsJson).HasColumnName("conditions_json").IsRequired();
        builder.Property(entity => entity.EffectsJson).HasColumnName("effects_json").IsRequired();
        builder.Property(entity => entity.IsTerminal).HasColumnName("is_terminal");
        builder.HasOne(entity => entity.SequenceDefinition)
            .WithMany(sequence => sequence.Steps)
            .HasForeignKey(entity => entity.SequenceDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entity => new { entity.SequenceDefinitionId, entity.Key }).IsUnique();
        builder.HasIndex(entity => new { entity.SequenceDefinitionId, entity.Order }).IsUnique();
    }
}
