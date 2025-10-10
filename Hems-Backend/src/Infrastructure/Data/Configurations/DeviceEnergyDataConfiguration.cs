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

        builder.Property(x => x.DeviceId)
            .IsRequired();

        builder.Property(x => x.DataDateTime)
            .IsRequired();

        builder.Property(x => x.DataType)
            .IsRequired()
            .HasMaxLength(50);

        // Enerji değerleri (kWh) - DataType'a göre günlük veya toplam değer
        builder.Property(x => x.PvEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GridImportEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GridExportEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.GeneratorEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.BatteryDischargeEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.BatteryChargeEnergyKWh)
            .HasPrecision(18, 3);

        builder.Property(x => x.LoadEnergyKWh)
            .HasPrecision(18, 3);

        // Foreign Key
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => x.DataDateTime);
        builder.HasIndex(x => x.DataType);
        builder.HasIndex(x => new { x.DeviceId, x.DataDateTime, x.DataType })
            .IsUnique();
    }
}
