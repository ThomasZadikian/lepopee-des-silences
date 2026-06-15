using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class MapNodeParentNodeEntityConfiguration : IEntityTypeConfiguration<MapNodeParentNodeEntity>
{
    public void Configure(EntityTypeBuilder<MapNodeParentNodeEntity> builder)
    {
        builder.ToTable("run_node_parent_nodes");

        builder.HasKey(link => new { link.MapNodeId, link.ParentNodeId });

        builder.Property(link => link.MapNodeId).HasColumnName("map_node_id");
        builder.Property(link => link.ParentNodeId).HasColumnName("parent_node_id");

        builder.HasOne(link => link.MapNode)
            .WithMany(node => node.ParentNodeLinks)
            .HasForeignKey(link => link.MapNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(link => link.MapNodeId);
        builder.HasIndex(link => link.ParentNodeId);
    }
}