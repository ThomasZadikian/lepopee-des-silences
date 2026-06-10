using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class PlayerRuntimeStateEntityConfiguration : IEntityTypeConfiguration<PlayerRuntimeStateEntity>
{
    public void Configure(EntityTypeBuilder<PlayerRuntimeStateEntity> builder)
    {
        builder.ToTable("run_player_states");

        builder.HasKey(s => s.RunId);

        builder.Property(s => s.RunId).HasColumnName("run_id");
        builder.Property(s => s.MaxVitality).HasColumnName("max_vitality");
        builder.Property(s => s.CurrentVitality).HasColumnName("current_vitality");
        builder.Property(s => s.Guard).HasColumnName("guard");
        builder.Property(s => s.Mana).HasColumnName("mana");
        builder.Property(s => s.Charge).HasColumnName("charge");

        builder.HasOne(s => s.Run)
            .WithOne(r => r.PlayerState)
            .HasForeignKey<PlayerRuntimeStateEntity>(s => s.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
