using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class ExternalDeviceConfiguration : IEntityTypeConfiguration<ExternalDevice>
{
    public void Configure(EntityTypeBuilder<ExternalDevice> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.DeviceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Specifications)
            .HasColumnType("jsonb"); // PostgreSQL JSONB column

        // Foreign key relationship
        builder.HasOne(ed => ed.SetupDevice)
            .WithMany(sd => sd.ExternalDevices)
            .HasForeignKey(ed => ed.SetupDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ed => ed.SetupDeviceId)
            .HasDatabaseName("IX_ExternalDevices_SetupDeviceId");

        builder.HasIndex(ed => ed.DeviceType)
            .HasDatabaseName("IX_ExternalDevices_DeviceType");
    }
}
