using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignStatusHistoryConfiguration : IEntityTypeConfiguration<CampaignStatusHistory>
    {
        public void Configure(EntityTypeBuilder<CampaignStatusHistory> builder)
        {
            builder.ToTable("campaignstatushistory");

            builder.Property(entity => entity.ChangedByUserName).HasMaxLength(150);
            builder.Property(entity => entity.Reason).HasMaxLength(500);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.CampaignId, entity.ChangedAt })
                .HasDatabaseName("ixcampaignstatushistorycampaignidchangedat");
        }
    }
}
