using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityApprovalCommentConfiguration : IEntityTypeConfiguration<OpportunityApprovalComment>
    {
        public void Configure(EntityTypeBuilder<OpportunityApprovalComment> builder)
        {
            builder.ToTable("opportunityapprovalcomment");

            builder.Property(entity => entity.UserName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Role)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(entity => entity.Body)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasOne(entity => entity.OpportunityApprovalRequest)
                .WithMany()
                .HasForeignKey(entity => entity.OpportunityApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.OpportunityApprovalRequestId, entity.CreatedAt })
                .HasDatabaseName("ixopportunityapprovalcommentrequestcreated");
        }
    }
}
