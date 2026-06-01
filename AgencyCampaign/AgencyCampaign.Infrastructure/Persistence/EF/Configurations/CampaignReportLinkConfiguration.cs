using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignReportLinkConfiguration : IEntityTypeConfiguration<CampaignReportLink>
    {
        public void Configure(EntityTypeBuilder<CampaignReportLink> builder)
        {
            builder.ToTable("campaignreportlink");

            builder.Property(entity => entity.Token).IsRequired().HasMaxLength(128);
            builder.Property(entity => entity.CreatedByUserName).HasMaxLength(150);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.Token)
                .IsUnique()
                .HasDatabaseName("uxcampaignreportlinktoken");

            builder.HasIndex(entity => entity.CampaignId)
                .HasDatabaseName("ixcampaignreportlinkcampaignid");
        }
    }
}
