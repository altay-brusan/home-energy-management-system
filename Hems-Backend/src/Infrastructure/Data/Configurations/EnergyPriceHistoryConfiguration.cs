using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class EnergyPriceHistoryConfiguration : IEntityTypeConfiguration<EnergyPriceHistory>
{
    public void Configure(EntityTypeBuilder<EnergyPriceHistory> builder)
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

        // Hour range: 0-23 for BeginHour, 0-24 for EndHour
        builder.Property(t => t.BeginHour);
        builder.Property(t => t.EndHour);

        builder.HasOne(t => t.Device)
            .WithMany()
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.DeviceId)
            .HasDatabaseName("IX_EnergyPriceHistory_DeviceId");

        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("IX_EnergyPriceHistory_IsActive");

        builder.HasIndex(d => new { d.DeviceId, d.IsActive })
            .HasDatabaseName("IX_EnergyPriceHistory_DeviceId_IsActive");
    }
}
