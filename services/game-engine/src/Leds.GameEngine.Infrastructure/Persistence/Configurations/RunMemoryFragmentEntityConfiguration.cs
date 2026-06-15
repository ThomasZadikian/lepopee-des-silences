using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunMemoryFragmentEntityConfiguration : IEntityTypeConfiguration<RunMemoryFragmentEntity>
{
    public void Configure(EntityTypeBuilder<RunMemoryFragmentEntity> builder)
    {
        builder.ToTable("run_memory_fragments");

        builder.HasKey(fragment => fragment.Id);

        builder.Property(fragment => fragment.Id).HasColumnName("id");
        builder.Property(fragment => fragment.RunId).HasColumnName("run_id");
        builder.Property(fragment => fragment.FragmentKey).HasColumnName("fragment_key").HasMaxLength(256).IsRequired();
        builder.Property(fragment => fragment.Order).HasColumnName("order");

        builder.HasOne(fragment => fragment.Run)
            .WithMany(run => run.MemoryFragments)
            .HasForeignKey(fragment => fragment.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fragment => fragment.RunId);
    }
}