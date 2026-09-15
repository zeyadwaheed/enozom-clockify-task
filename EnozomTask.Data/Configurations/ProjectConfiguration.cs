using EnozomTask.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnozomTask.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(project => project.Id);

        builder.Property(project => project.ClockifyId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(project => project.ClockifyId).IsUnique();

        builder.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(500);
    }
}