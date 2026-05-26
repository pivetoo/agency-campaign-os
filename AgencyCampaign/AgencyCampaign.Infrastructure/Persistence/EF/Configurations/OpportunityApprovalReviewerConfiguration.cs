using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityApprovalReviewerConfiguration : IEntityTypeConfiguration<OpportunityApprovalReviewer>
    {
        public void Configure(EntityTypeBuilder<OpportunityApprovalReviewer> builder)
        {
            builder.ToTable("opportunityapprovalreviewer");

            builder.Property(entity => entity.UserName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Role)
                .HasMaxLength(120);

            builder.Property(entity => entity.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(entity => entity.DecisionNotes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.OpportunityApprovalRequest)
                .WithMany(entity => entity.Reviewers)
                .HasForeignKey(entity => entity.OpportunityApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.OpportunityApprovalRequestId, entity.UserId })
                .HasDatabaseName("ixopportunityapprovalreviewerrequestuser");
        }
    }
}
