using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.Property(t => t.Manufacturer)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.ModelNumber)
            .HasMaxLength(100);

        builder.Property(t => t.SerialNumber)
            .HasMaxLength(100);

        builder.Property(t => t.TypeNumber)
            .HasMaxLength(100);

        builder.Property(t => t.QRCode)
            .HasMaxLength(100);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(t => t.Specifications)
            .HasColumnType("jsonb"); // PostgreSQL JSONB column

        builder.HasIndex(d => d.Manufacturer)
            .HasDatabaseName("IX_Devices_Manufacturer");

        builder.HasIndex(d => d.IsDeleted)
            .HasDatabaseName("IX_Devices_IsDeleted");

        builder.HasIndex(d => d.SerialNumber)
            .HasDatabaseName("IX_Devices_SerialNumber");

        builder.HasIndex(d => d.QRCode)
            .HasDatabaseName("IX_Devices_QRCode");
    }
}





