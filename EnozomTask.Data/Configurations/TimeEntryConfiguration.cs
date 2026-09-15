using EnozomTask.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnozomTask.Data.Configurations;

public class TimeEntryConfiguration
    : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.ClockifyId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(entry => entry.ClockifyId).IsUnique();

        builder.Property(entry => entry.StartUtc)
            .HasColumnType("datetime(6)");

        builder.Property(entry => entry.EndUtc)
            .HasColumnType("datetime(6)");

        builder.Property(entry => entry.Description)
            .HasColumnType("text");

        builder.HasOne(entry => entry.ProjectTask)
            .WithMany(task => task.TimeEntries)
            .HasForeignKey(entry => entry.ProjectTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
