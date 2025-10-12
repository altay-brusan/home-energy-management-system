using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class UserSiteConfiguration : IEntityTypeConfiguration<UserSite>
{
    public void Configure(EntityTypeBuilder<UserSite> builder)
    {
        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450); // For Identity User ID length

        builder.Property(t => t.Role)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => new { x.UserId, x.SiteId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Role });

        // Relationship with Site
        builder.HasOne(us => us.Site)
            .WithMany()
            .HasForeignKey(us => us.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

