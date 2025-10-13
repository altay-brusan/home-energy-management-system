using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceNotificationTypeConfiguration : IEntityTypeConfiguration<DeviceNotificationType>
{
    public void Configure(EntityTypeBuilder<DeviceNotificationType> builder)
    {
        builder.Property(t => t.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Message)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes for performance
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsActive);
    }
}
