using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RoomEntityConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> builder)
    {
        builder.ToTable("run_rooms");

        builder.HasKey(room => room.Id);

        builder.Property(room => room.Id).HasColumnName("id");
        builder.Property(room => room.RunId).HasColumnName("run_id");
        builder.Property(room => room.Depth).HasColumnName("depth");
        builder.Property(room => room.RoomType).HasColumnName("room_type").HasMaxLength(64).IsRequired();
        builder.Property(room => room.Theme).HasColumnName("theme").HasMaxLength(128).IsRequired();
        builder.Property(room => room.BossId).HasColumnName("boss_id").HasMaxLength(128).IsRequired();
        builder.Property(room => room.BossName).HasColumnName("boss_name").HasMaxLength(256).IsRequired();
        builder.Property(room => room.BossRoomType).HasColumnName("boss_room_type").HasMaxLength(64).IsRequired();
        builder.Property(room => room.BossDangerHint).HasColumnName("boss_danger_hint").HasMaxLength(512).IsRequired();
        builder.Property(room => room.BossEnemyTemplateKey).HasColumnName("boss_enemy_template_key").HasMaxLength(128).IsRequired();
        builder.Property(room => room.State).HasColumnName("state").HasMaxLength(64).IsRequired();
        builder.Property(room => room.CurrentNodeDepth).HasColumnName("current_node_depth");
        builder.Property(room => room.MaxNodeDepth).HasColumnName("max_node_depth");
        builder.Property(room => room.LayoutTemplateKey).HasColumnName("layout_template_key").HasMaxLength(128);
        builder.Property(room => room.LayoutTemplateVersion).HasColumnName("layout_template_version").HasMaxLength(64);

        builder.HasOne(room => room.Run)
            .WithMany(run => run.Rooms)
            .HasForeignKey(room => room.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(room => room.RunId);
        builder.HasIndex(room => room.State);
    }
}