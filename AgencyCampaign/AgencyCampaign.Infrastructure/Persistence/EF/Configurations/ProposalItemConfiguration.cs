using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalItemConfiguration : IEntityTypeConfiguration<ProposalItem>
    {
        public void Configure(EntityTypeBuilder<ProposalItem> builder)
        {
            builder.ToTable("proposalitem");

            builder.Property(entity => entity.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(entity => entity.Quantity)
                .IsRequired();

            builder.Property(entity => entity.UnitPrice)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.Observations)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.Proposal)
                .WithMany(proposal => proposal.Items)
                .HasForeignKey(entity => entity.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}