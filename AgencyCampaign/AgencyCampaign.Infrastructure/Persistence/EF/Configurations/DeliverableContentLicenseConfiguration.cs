using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableContentLicenseConfiguration : IEntityTypeConfiguration<DeliverableContentLicense>
    {
        public void Configure(EntityTypeBuilder<DeliverableContentLicense> builder)
        {
            builder.ToTable("deliverablecontentlicense");

            builder.Property(entity => entity.Channels).HasMaxLength(500);
            builder.Property(entity => entity.Notes).HasMaxLength(2000);
            builder.Property(entity => entity.Value).HasPrecision(14, 2);

            builder.HasOne<CampaignDeliverable>()
                .WithMany()
                .HasForeignKey(entity => entity.CampaignDeliverableId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.CampaignDeliverableId);
            builder.HasIndex(entity => entity.ExpiresAt);
        }
    }
}
