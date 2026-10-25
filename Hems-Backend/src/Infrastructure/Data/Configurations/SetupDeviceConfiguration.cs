using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class SetupDeviceConfiguration : IEntityTypeConfiguration<SetupDevice>
{
    public void Configure(EntityTypeBuilder<SetupDevice> builder)
    {
        // Foreign key relationships
        builder.HasOne(sd => sd.Setup)
            .WithOne(s => s.SetupDevice)
            .HasForeignKey<SetupDevice>(sd => sd.SetupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.Device)
            .WithOne(d => d.SetupDevice)
            .HasForeignKey<SetupDevice>(sd => sd.DeviceId)
            .OnDelete(DeleteBehavior.Restrict); // Device silinirse SetupDevice silinmez

        // Unique constraint: Bir Setup'ta aynı Device sadece bir kez olabilir
        builder.HasIndex(sd => new { sd.SetupId, sd.DeviceId })
            .IsUnique()
            .HasDatabaseName("IX_SetupDevices_SetupId_DeviceId");

        // Unique constraint: Bir Setup'ta sadece bir SetupDevice olabilir
        builder.HasIndex(sd => sd.SetupId)
            .IsUnique()
            .HasDatabaseName("IX_SetupDevices_SetupId_Unique");

        // Indexes
        builder.HasIndex(sd => sd.DeviceId)
            .HasDatabaseName("IX_SetupDevices_DeviceId");
    }
}
