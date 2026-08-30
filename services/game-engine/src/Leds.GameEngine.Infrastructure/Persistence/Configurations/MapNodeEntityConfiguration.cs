using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class MapNodeEntityConfiguration : IEntityTypeConfiguration<MapNodeEntity>
{
    public void Configure(EntityTypeBuilder<MapNodeEntity> builder)
    {
        builder.ToTable("run_nodes");

        builder.HasKey(node => node.Id);

        builder.Property(node => node.Id).HasColumnName("id");
        builder.Property(node => node.RoomId).HasColumnName("room_id");
        builder.Property(node => node.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
        builder.Property(node => node.Row).HasColumnName("row");
        builder.Property(node => node.Lane).HasColumnName("lane");
        builder.Property(node => node.RiskLevel).HasColumnName("risk_level");
        builder.Property(node => node.CombatRiskTier).HasColumnName("combat_risk_tier").HasMaxLength(32);
        builder.Property(node => node.RewardProfile).HasColumnName("reward_profile").HasMaxLength(128).IsRequired();
        builder.Property(node => node.IsBoss).HasColumnName("is_boss");
        builder.Property(node => node.State).HasColumnName("state").HasMaxLength(64).IsRequired();
        builder.Property(node => node.ChosenEventOptionId).HasColumnName("chosen_event_option_id").HasMaxLength(128);
        builder.Property(node => node.HiddenState).HasColumnName("hidden_state").HasMaxLength(32);
        builder.Property(node => node.DangerTell).HasColumnName("danger_tell").HasMaxLength(32);
        builder.Property(node => node.ContactBehavior).HasColumnName("contact_behavior").HasMaxLength(32);
        builder.Property(node => node.ExitDestinationRoomKey).HasColumnName("exit_destination_room_key").HasMaxLength(160);
        builder.Property(node => node.ExitDestinationDisplayName).HasColumnName("exit_destination_display_name").HasMaxLength(160);

        builder.HasOne(node => node.Room)
            .WithMany(room => room.Nodes)
            .HasForeignKey(node => node.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(node => node.RoomId);
        builder.HasIndex(node => node.State);
        builder.HasIndex(node => node.Row);
    }
}