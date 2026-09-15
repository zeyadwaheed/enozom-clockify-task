using EnozomTask.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnozomTask.Data.Configurations;

public class ProjectTaskConfiguration
    : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.HasKey(task => task.Id);

        builder.Property(task => task.ClockifyId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(task => task.ClockifyId).IsUnique();


        builder.Property(task => task.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(task => task.OriginalEstimateHours)
            .HasPrecision(12, 4);

        builder.HasOne(task => task.Project)
            .WithMany(project => project.Tasks)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.AssignedUser)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(task => task.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
