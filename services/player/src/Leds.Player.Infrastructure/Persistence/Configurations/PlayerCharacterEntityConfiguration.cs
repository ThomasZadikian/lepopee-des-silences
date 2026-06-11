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
        builder.Property(c => c.DefinitionKey).HasColumnName("definition_key").HasMaxLength(128).IsRequired();
        builder.Property(c => c.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
        builder.Property(c => c.MaxVitality).HasColumnName("max_vitality");
        builder.Property(c => c.BaseMana).HasColumnName("base_mana");
        builder.Property(c => c.BaseCharge).HasColumnName("base_charge");
        builder.Property(c => c.SkillKeysJson).HasColumnName("skill_keys_json");
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");

        builder.HasOne(c => c.PlayerProfile)
            .WithMany(p => p.Characters)
            .HasForeignKey(c => c.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PlayerProfileId);
    }
}
