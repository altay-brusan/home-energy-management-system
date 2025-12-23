using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class DevicePriceConfiguration : IEntityTypeConfiguration<DevicePrice>
{
    public void Configure(EntityTypeBuilder<DevicePrice> builder)
    {
        builder.Property(t => t.Unit)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("kWh");

        builder.Property(t => t.Currency)
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("EUR");

        builder.Property(t => t.SellPrice)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(t => t.BuyPrice)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        // Hour range validation: 0-23
        builder.Property(t => t.BeginHour);
        builder.Property(t => t.EndHour);

        builder.HasOne(t => t.Device)
            .WithMany()
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.DeviceId)
            .HasDatabaseName("IX_DevicePrices_DeviceId");

        builder.HasIndex(d => d.EffectiveFrom)
            .HasDatabaseName("IX_DevicePrices_EffectiveFrom");

        builder.HasIndex(d => new { d.DeviceId, d.EffectiveFrom })
            .HasDatabaseName("IX_DevicePrices_DeviceId_EffectiveFrom");

        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("IX_DevicePrices_IsActive");
    }
}
