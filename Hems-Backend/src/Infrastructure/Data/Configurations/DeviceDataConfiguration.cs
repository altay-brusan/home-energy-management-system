using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceDataConfiguration : IEntityTypeConfiguration<Hems_Backend.Domain.Entities.DeviceData>
{
    public void Configure(EntityTypeBuilder<Hems_Backend.Domain.Entities.DeviceData> builder)
    {
        builder.ToTable("DeviceData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DataType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ThingsBoardDeviceId)
            .IsRequired();

        builder.Property(x => x.RequestDate)
            .IsRequired();

        builder.Property(x => x.Data)
            .IsRequired()
            .HasColumnType("jsonb");

        // Performance indexes for high-frequency queries
        builder.HasIndex(x => x.ThingsBoardDeviceId)
            .HasDatabaseName("IX_DeviceData_DeviceId");

        builder.HasIndex(x => x.RequestDate)
            .HasDatabaseName("IX_DeviceData_RequestDate");

        builder.HasIndex(x => x.DataType)
            .HasDatabaseName("IX_DeviceData_DataType");

        // Composite index for common query patterns
        builder.HasIndex(x => new { x.ThingsBoardDeviceId, x.RequestDate })
            .HasDatabaseName("IX_DeviceData_DeviceId_RequestDate");

        builder.HasIndex(x => new { x.ThingsBoardDeviceId, x.DataType, x.RequestDate })
            .HasDatabaseName("IX_DeviceData_DeviceId_DataType_RequestDate");

        // Foreign key relationship - ThingsBoardDeviceId in DeviceData references Id in Device table
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.ThingsBoardDeviceId)
            .HasPrincipalKey(d => d.ThingsBoardDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(x => x.Created)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.LastModified)
            .IsRequired();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256);
    }
}


