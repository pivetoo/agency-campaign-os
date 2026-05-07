using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignCreatorStatusHistoryConfiguration : IEntityTypeConfiguration<CampaignCreatorStatusHistory>
    {
        public void Configure(EntityTypeBuilder<CampaignCreatorStatusHistory> builder)
        {
            builder.ToTable("campaigncreatorstatushistory");

            builder.Property(entity => entity.ChangedByUserName).HasMaxLength(150);
            builder.Property(entity => entity.Reason).HasMaxLength(500);

            builder.HasOne(entity => entity.CampaignCreator)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignCreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entity => entity.FromStatus)
                .WithMany()
                .HasForeignKey(entity => entity.FromStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.ToStatus)
                .WithMany()
                .HasForeignKey(entity => entity.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => new { entity.CampaignCreatorId, entity.ChangedAt })
                .HasDatabaseName("ixcampaigncreatorstatushistorycampaigncreatoridchangedat");
        }
    }
}
