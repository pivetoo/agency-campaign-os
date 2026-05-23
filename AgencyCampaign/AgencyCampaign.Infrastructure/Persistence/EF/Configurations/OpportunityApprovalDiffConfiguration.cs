using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityApprovalDiffConfiguration : IEntityTypeConfiguration<OpportunityApprovalDiff>
    {
        public void Configure(EntityTypeBuilder<OpportunityApprovalDiff> builder)
        {
            builder.ToTable("opportunityapprovaldiff");

            builder.Property(entity => entity.Field)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.PolicyValue)
                .HasMaxLength(200);

            builder.Property(entity => entity.RequestedValue)
                .HasMaxLength(200);

            builder.Property(entity => entity.Delta)
                .HasMaxLength(120);

            builder.Property(entity => entity.Kind)
                .HasConversion<int>()
                .IsRequired();

            builder.HasOne(entity => entity.OpportunityApprovalRequest)
                .WithMany()
                .HasForeignKey(entity => entity.OpportunityApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.OpportunityApprovalRequestId, entity.DisplayOrder })
                .HasDatabaseName("ixopportunityapprovaldiffrequestorder");
        }
    }
}
