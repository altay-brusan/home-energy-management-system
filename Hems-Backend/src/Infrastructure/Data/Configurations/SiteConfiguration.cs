using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Country)
            .HasMaxLength(100);

        builder.Property(t => t.City)
            .HasMaxLength(100);

        builder.Property(t => t.AddressLine)
            .HasMaxLength(500);

        builder.Property(t => t.Latitude)
            .HasPrecision(9, 6);

        builder.Property(t => t.Longitude)
            .HasPrecision(9, 6);

        builder.Property(t => t.Timezone)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Europe/Istanbul");

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450); // For Identity User ID length

        // Index for performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.UserId, x.IsDeleted });

        // 1:1 relationship with Setup - Bir Site'ın sadece bir Setup'ı olabilir
        builder.HasOne(s => s.Setup)
            .WithOne(st => st.Site)
            .HasForeignKey<Setup>(st => st.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}









