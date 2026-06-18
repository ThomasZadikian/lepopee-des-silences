using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class NpcDefinitionEntityConfiguration : IEntityTypeConfiguration<NpcDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<NpcDefinitionEntity> builder)
    {
        builder.ToTable("catalog_npc_definitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(e => e.MinDepth).HasColumnName("min_depth");
        builder.Property(e => e.MaxDepth).HasColumnName("max_depth");
        builder.Property(e => e.CompatibleRoomTypesJson).HasColumnName("compatible_room_types_json");
        builder.Property(e => e.CompatiblePalaceRoomStatesJson).HasColumnName("compatible_palace_room_states_json");
        builder.Property(e => e.CompatibleRoomClimatesJson).HasColumnName("compatible_room_climates_json");
        builder.Property(e => e.TagsJson).HasColumnName("tags_json");
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}
