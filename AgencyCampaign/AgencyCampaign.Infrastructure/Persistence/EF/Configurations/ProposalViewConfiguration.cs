using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalViewConfiguration : IEntityTypeConfiguration<ProposalView>
    {
        public void Configure(EntityTypeBuilder<ProposalView> builder)
        {
            builder.ToTable("proposalview");

            builder.Property(entity => entity.IpAddress)
                .HasMaxLength(64);

            builder.Property(entity => entity.UserAgent)
                .HasMaxLength(500);

            builder.HasIndex(entity => new { entity.ProposalShareLinkId, entity.ViewedAt })
                .HasDatabaseName("ixproposalviewproposalsharelinkidviewedat");
        }
    }
}
