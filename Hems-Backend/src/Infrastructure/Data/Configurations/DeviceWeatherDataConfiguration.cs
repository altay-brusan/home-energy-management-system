using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DeviceWeatherDataConfiguration : IEntityTypeConfiguration<DeviceWeatherData>
{
    public void Configure(EntityTypeBuilder<DeviceWeatherData> builder)
    {
        builder.ToTable("DeviceWeatherData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ThingsBoardDeviceId)
            .IsRequired();

        builder.Property(x => x.DeviceLocalTime)
            .IsRequired();

        builder.Property(x => x.Temperature)
            .HasPrecision(5, 2);

        builder.Property(x => x.TemperatureCondition)
            .HasMaxLength(100);

        builder.Property(x => x.Humidity)
            .HasPrecision(5, 2);

        builder.Property(x => x.WindSpeed)
            .HasPrecision(6, 2);

        builder.Property(x => x.WindDirection)
            .HasMaxLength(50);

        builder.Property(x => x.Pressure)
            .HasPrecision(8, 2);

        builder.Property(x => x.Condition)
            .HasMaxLength(50);

        builder.Property(x => x.CloudCover)
            .HasPrecision(5, 2);

        builder.Property(x => x.UvIndex)
            .HasPrecision(4, 2);

        // Foreign Key - ThingsBoardDeviceId references Device.ThingsBoardDeviceId
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.ThingsBoardDeviceId)
            .HasPrincipalKey(d => d.ThingsBoardDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.ThingsBoardDeviceId);
        
        builder.HasIndex(x => new { x.ThingsBoardDeviceId, x.DeviceLocalTime })
            .HasDatabaseName("IX_DeviceWeatherData_ThingsBoardDeviceId_DeviceLocalTime");
    }
}
