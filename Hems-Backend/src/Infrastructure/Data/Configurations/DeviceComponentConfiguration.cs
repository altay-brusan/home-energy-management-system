using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceComponentConfiguration : IEntityTypeConfiguration<DeviceComponent>
{
    public void Configure(EntityTypeBuilder<DeviceComponent> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Manufacturer)
            .HasMaxLength(100);

        builder.Property(t => t.ModelNumber)
            .HasMaxLength(100);

        builder.Property(t => t.SerialNumber)
            .HasMaxLength(100);

        builder.Property(t => t.Specifications)
            .HasColumnType("jsonb"); // PostgreSQL JSONB column

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Foreign key relationship
        builder.HasOne(dc => dc.Device)
            .WithMany(d => d.DeviceComponents)
            .HasForeignKey(dc => dc.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(dc => dc.DeviceId)
            .HasDatabaseName("IX_DeviceComponents_DeviceId");

        builder.HasIndex(dc => dc.DeviceType)
            .HasDatabaseName("IX_DeviceComponents_DeviceType");

        builder.HasIndex(dc => dc.IsDeleted)
            .HasDatabaseName("IX_DeviceComponents_IsDeleted");
    }
}
