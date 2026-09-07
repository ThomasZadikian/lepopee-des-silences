using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunCharacterSnapshotEntityConfiguration : IEntityTypeConfiguration<RunCharacterSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<RunCharacterSnapshotEntity> builder)
    {
        builder.ToTable("run_character_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PlayerSnapshotId).HasColumnName("player_snapshot_id");
        builder.Property(e => e.CharacterId).HasColumnName("character_id");
        builder.Property(e => e.DefinitionKey).HasColumnName("definition_key").HasMaxLength(160).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(e => e.EmotionalRegisterCode).HasColumnName("emotional_register_code").HasMaxLength(32).IsRequired();
        builder.Property(e => e.CurrentVitality).HasColumnName("current_vitality");
        builder.Property(e => e.CurrentMana).HasColumnName("current_mana");
        builder.Property(e => e.SnapshotOrder).HasColumnName("snapshot_order").HasDefaultValue(0);
        builder.Property(e => e.EquippedItemKeysCsv).HasColumnName("equipped_item_keys_csv");
        builder.Property(e => e.EquipmentLoadoutJson).HasColumnName("equipment_loadout_json");

        builder.HasIndex(e => e.PlayerSnapshotId);

        builder.HasOne(e => e.StatBlock)
            .WithOne(e => e.CharacterSnapshot)
            .HasForeignKey<RunCharacterStatSnapshotEntity>(e => e.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Skills)
            .WithOne(e => e.CharacterSnapshot)
            .HasForeignKey(e => e.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
