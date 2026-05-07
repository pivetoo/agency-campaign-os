using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalStatusHistoryConfiguration : IEntityTypeConfiguration<ProposalStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ProposalStatusHistory> builder)
        {
            builder.ToTable("proposalstatushistory");

            builder.Property(entity => entity.FromStatus)
                .HasConversion<int?>();

            builder.Property(entity => entity.ToStatus)
                .HasConversion<int>();

            builder.Property(entity => entity.ChangedByUserName)
                .HasMaxLength(255);

            builder.Property(entity => entity.Reason)
                .HasMaxLength(500);

            builder.HasIndex(entity => new { entity.ProposalId, entity.ChangedAt })
                .HasDatabaseName("ixproposalstatushistoryproposalidchangedat");
        }
    }
}
