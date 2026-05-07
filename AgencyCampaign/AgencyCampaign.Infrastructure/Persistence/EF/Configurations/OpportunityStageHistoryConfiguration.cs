using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityStageHistoryConfiguration : IEntityTypeConfiguration<OpportunityStageHistory>
    {
        public void Configure(EntityTypeBuilder<OpportunityStageHistory> builder)
        {
            builder.ToTable("opportunitystagehistory");

            builder.Property(entity => entity.ChangedByUserName)
                .HasMaxLength(255);

            builder.Property(entity => entity.Reason)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.FromStage)
                .WithMany()
                .HasForeignKey(entity => entity.FromStageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.ToStage)
                .WithMany()
                .HasForeignKey(entity => entity.ToStageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => new { entity.OpportunityId, entity.ChangedAt })
                .HasDatabaseName("ixopportunitystagehistoryopportunityidchangedat");
        }
    }
}
