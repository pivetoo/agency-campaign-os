using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityApprovalRequestConfiguration : IEntityTypeConfiguration<OpportunityApprovalRequest>
    {
        public void Configure(EntityTypeBuilder<OpportunityApprovalRequest> builder)
        {
            builder.ToTable("opportunityapprovalrequest");

            builder.Property(entity => entity.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(entity => entity.RequestedByUserName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.ApprovedByUserName)
                .HasMaxLength(150);

            builder.Property(entity => entity.DecisionNotes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.OpportunityNegotiation)
                .WithMany(entity => entity.ApprovalRequests)
                .HasForeignKey(entity => entity.OpportunityNegotiationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
