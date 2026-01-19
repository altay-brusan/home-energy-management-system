using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class MobileAppVersionConfiguration : IEntityTypeConfiguration<MobileAppVersion>
{
    public void Configure(EntityTypeBuilder<MobileAppVersion> builder)
    {
        builder.Property(t => t.Platform)
            .IsRequired();

        builder.Property(t => t.Version)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.VersionMajor)
            .IsRequired();

        builder.Property(t => t.VersionMinor)
            .IsRequired();

        builder.Property(t => t.VersionPatch)
            .IsRequired();

        builder.Property(t => t.StoreUrl)
            .HasMaxLength(500);

        builder.Property(t => t.ReleaseNotes)
            .HasMaxLength(2000);

        builder.Property(t => t.MinimumRequiredVersion)
            .HasMaxLength(50);

        builder.Property(t => t.RevocationReason)
            .HasMaxLength(500);

        builder.Property(t => t.IsMandatory)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsLatest)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes for performance
        builder.HasIndex(x => x.Platform);
        builder.HasIndex(x => x.IsLatest);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.Platform, x.IsLatest });
        builder.HasIndex(x => new { x.Platform, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.Platform, x.VersionMajor, x.VersionMinor, x.VersionPatch });
    }
}
