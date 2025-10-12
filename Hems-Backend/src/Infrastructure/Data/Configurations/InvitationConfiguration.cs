using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hems_Backend.Infrastructure.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.Property(t => t.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.InvitedUserId)
            .HasMaxLength(450); // For Identity User ID length

        builder.Property(t => t.InvitedByUserId)
            .IsRequired()
            .HasMaxLength(450); // For Identity User ID length

        builder.Property(t => t.Token)
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.Token);
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => x.InvitedUserId);
        builder.HasIndex(x => new { x.Email, x.SiteId });
        builder.HasIndex(x => new { x.Token, x.Status });

        // Relationship with Site
        builder.HasOne(i => i.Site)
            .WithMany()
            .HasForeignKey(i => i.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

