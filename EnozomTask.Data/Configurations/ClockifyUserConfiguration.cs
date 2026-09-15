using EnozomTask.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnozomTask.Data.Configurations;

public class ClockifyUserConfiguration
    : IEntityTypeConfiguration<ClockifyUser>
{
    public void Configure(EntityTypeBuilder<ClockifyUser> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.ClockifyId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(user => user.ClockifyId).IsUnique();

        builder.Property(user => user.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}