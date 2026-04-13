using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignDeliverableConfiguration : IEntityTypeConfiguration<CampaignDeliverable>
    {
        public void Configure(EntityTypeBuilder<CampaignDeliverable> builder)
        {
            builder.ToTable("campaign_deliverable");

            builder.Property(entity => entity.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.GrossAmount)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.CreatorAmount)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.AgencyFeeAmount)
                .HasPrecision(18, 2);

            builder.HasOne(entity => entity.Campaign)
                .WithMany(entity => entity.Deliverables)
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
