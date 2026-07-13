using Leds.Player.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Player.Infrastructure.Persistence.Configurations;

public sealed class PlayerCharacterEntityConfiguration : IEntityTypeConfiguration<PlayerCharacterEntity>
{
    public void Configure(EntityTypeBuilder<PlayerCharacterEntity> builder)
    {
        builder.ToTable("player_characters");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.PlayerProfileId).HasColumnName("player_profile_id");
        builder.Property(c => c.DefinitionKey).HasColumnName("definition_key").HasMaxLength(160).IsRequired();
        builder.Property(c => c.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(c => c.CharacterType).HasColumnName("character_type").HasMaxLength(64).HasDefaultValue("Standard").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(32).HasDefaultValue("Active").IsRequired();
        builder.Property(c => c.MaxVitality).HasColumnName("max_vitality").HasComment("Legacy compatibility column. Use player_character_stat_blocks.max_vitality for data-model-0.1.");
        builder.Property(c => c.BaseMana).HasColumnName("base_mana").HasComment("Legacy compatibility column. Use player_character_stat_blocks.mana for data-model-0.1.");
        builder.Property(c => c.BaseCharge).HasColumnName("base_charge").HasComment("Legacy compatibility column. Use player_character_stat_blocks.charge for data-model-0.1.");
        builder.Property(c => c.StatPointsInvested).HasColumnName("stat_points_invested").HasDefaultValue(0);
        builder.Property(c => c.SkillKeysJson).HasColumnName("skill_keys_json").HasComment("Legacy compatibility column. Use player_character_skills for data-model-0.1.");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasOne(c => c.PlayerProfile)
            .WithMany(p => p.Characters)
            .HasForeignKey(c => c.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PlayerProfileId);
        builder.HasIndex(c => new { c.PlayerProfileId, c.DefinitionKey }).IsUnique();
    }
}
