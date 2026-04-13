using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
    {
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.ToTable("campaign");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.Objective)
                .HasMaxLength(500);

            builder.Property(entity => entity.Briefing)
                .HasMaxLength(4000);

            builder.Property(entity => entity.InternalOwnerName)
                .HasMaxLength(150);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.Budget)
                .HasPrecision(18, 2);

            builder.HasOne(entity => entity.Brand)
                .WithMany()
                .HasForeignKey(entity => entity.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
