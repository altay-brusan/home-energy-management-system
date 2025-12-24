using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DevicePowerDataConfiguration : IEntityTypeConfiguration<DevicePowerData>
{
    public void Configure(EntityTypeBuilder<DevicePowerData> builder)
    {
        builder.ToTable("DevicePowerData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThingsBoardDeviceId)
            .IsRequired();

        builder.Property(x => x.DeviceLocalTime)
            .IsRequired();

        builder.Property(x => x.DataType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.MessageType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PhaseType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("SinglePhase");

        builder.Property(x => x.Version);

        // Power values (W)
        builder.Property(x => x.PvTotalPowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt1PowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt2PowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt3PowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt4PowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.GridTotalPowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.LoadTotalPowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.BatteryPowerW)
            .HasPrecision(18, 3);

        builder.Property(x => x.GeneratorTotalPowerW)
            .HasPrecision(18, 3);

        // Foreign Key - ThingsBoardDeviceId references Device.ThingsBoardDeviceId
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.ThingsBoardDeviceId)
            .HasPrincipalKey(d => d.ThingsBoardDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ThingsBoardDeviceId);
        
        builder.HasIndex(x => new { x.ThingsBoardDeviceId, x.DeviceLocalTime, x.DataType })
            .HasDatabaseName("IX_DevicePowerData_ThingsBoardDeviceId_DeviceLocalTime_DataType");

        builder.HasIndex(x => x.DeviceLocalTime)
            .HasDatabaseName("IX_DevicePowerData_DeviceLocalTime");
    }
}
