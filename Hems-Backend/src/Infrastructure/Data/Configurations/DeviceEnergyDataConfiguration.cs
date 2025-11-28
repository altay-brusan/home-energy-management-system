using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceEnergyDataConfiguration : IEntityTypeConfiguration<DeviceEnergyData>
{
    public void Configure(EntityTypeBuilder<DeviceEnergyData> builder)
    {
        builder.ToTable("DeviceEnergyData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThingsBoardDeviceId)
            .IsRequired();

        builder.Property(x => x.DataDateTime)
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

        // Energy values (kWh)
        builder.Property(x => x.LoadEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GridImportEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GridExportEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvTotalEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt1EnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt2EnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt3EnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.PvMppt4EnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.BatteryChargeEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.BatteryDischargeEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GeneratorEnergyKWh)
            .HasPrecision(18, 3);

        // Foreign Key - ThingsBoardDeviceId references Device.ThingsBoardDeviceId
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.ThingsBoardDeviceId)
            .HasPrincipalKey(d => d.ThingsBoardDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ThingsBoardDeviceId);
        
        builder.HasIndex(x => new { x.ThingsBoardDeviceId, x.DataDateTime, x.DataType })
            .HasDatabaseName("IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType");

        builder.HasIndex(x => x.DataDateTime)
            .HasDatabaseName("IX_DeviceEnergyData_DataDateTime");
    }
}
