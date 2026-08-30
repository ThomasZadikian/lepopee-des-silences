using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class LocalRuleStateEntityConfiguration : IEntityTypeConfiguration<LocalRuleStateEntity>
{
    public void Configure(EntityTypeBuilder<LocalRuleStateEntity> builder)
    {
        builder.ToTable("run_room_local_rule_states");

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id).HasColumnName("id");
        builder.Property(state => state.RoomId).HasColumnName("room_id");
        builder.Property(state => state.LocalRuleKey).HasColumnName("local_rule_key").HasMaxLength(160).IsRequired();
        builder.Property(state => state.CumulativeSeverity).HasColumnName("cumulative_severity");
        builder.Property(state => state.HasBeenInformed).HasColumnName("has_been_informed");
        builder.Property(state => state.TriggeredThresholdsCsv).HasColumnName("triggered_thresholds_csv");

        builder.HasOne(state => state.Room)
            .WithMany(room => room.LocalRuleStates)
            .HasForeignKey(state => state.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(state => state.RoomId);
    }
}
