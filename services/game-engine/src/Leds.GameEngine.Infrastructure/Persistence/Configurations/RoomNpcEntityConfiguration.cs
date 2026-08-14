using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RoomNpcEntityConfiguration : IEntityTypeConfiguration<RoomNpcEntity>
{
    public void Configure(EntityTypeBuilder<RoomNpcEntity> builder)
    {
        builder.ToTable("run_room_npcs");

        builder.HasKey(npc => npc.Id);

        builder.Property(npc => npc.Id).HasColumnName("id");
        builder.Property(npc => npc.RoomId).HasColumnName("room_id");
        builder.Property(npc => npc.CatalogNpcKey).HasColumnName("catalog_npc_key").HasMaxLength(160).IsRequired();
        builder.Property(npc => npc.OriginX).HasColumnName("origin_x");
        builder.Property(npc => npc.OriginY).HasColumnName("origin_y");
        builder.Property(npc => npc.X).HasColumnName("x");
        builder.Property(npc => npc.Y).HasColumnName("y");
        builder.Property(npc => npc.Behavior).HasColumnName("behavior").HasMaxLength(32).IsRequired();
        builder.Property(npc => npc.Awareness).HasColumnName("awareness").HasMaxLength(32).IsRequired();
        builder.Property(npc => npc.AwarenessRadius).HasColumnName("awareness_radius");
        builder.Property(npc => npc.WaypointsCsv).HasColumnName("waypoints_csv");
        builder.Property(npc => npc.WaypointIndex).HasColumnName("waypoint_index");
        builder.Property(npc => npc.StepCount).HasColumnName("step_count");

        builder.HasOne(npc => npc.Room)
            .WithMany(room => room.RoomNpcs)
            .HasForeignKey(npc => npc.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(npc => npc.RoomId);
    }
}
