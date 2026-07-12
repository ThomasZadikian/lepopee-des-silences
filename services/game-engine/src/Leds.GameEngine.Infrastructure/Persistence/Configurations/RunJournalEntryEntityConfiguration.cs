using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.GameEngine.Infrastructure.Persistence.Configurations;

public sealed class RunJournalEntryEntityConfiguration : IEntityTypeConfiguration<RunJournalEntryEntity>
{
    public void Configure(EntityTypeBuilder<RunJournalEntryEntity> builder)
    {
        builder.ToTable("run_journal_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).HasColumnName("id");
        builder.Property(entry => entry.RunId).HasColumnName("run_id");
        builder.Property(entry => entry.RoomIndex).HasColumnName("room_index");
        builder.Property(entry => entry.RoomDisplayName).HasColumnName("room_display_name").HasMaxLength(256);
        builder.Property(entry => entry.Text).HasColumnName("text").HasMaxLength(1024).IsRequired();
        builder.Property(entry => entry.Order).HasColumnName("order");

        builder.HasOne(entry => entry.Run)
            .WithMany(run => run.JournalEntries)
            .HasForeignKey(entry => entry.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entry => entry.RunId);
    }
}
