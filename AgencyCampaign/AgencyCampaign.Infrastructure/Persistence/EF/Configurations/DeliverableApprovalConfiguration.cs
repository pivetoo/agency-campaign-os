using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableApprovalConfiguration : IEntityTypeConfiguration<DeliverableApproval>
    {
        public void Configure(EntityTypeBuilder<DeliverableApproval> builder)
        {
            builder.ToTable("deliverableapproval");

            builder.Property(entity => entity.ReviewerName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Comment)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.CampaignDeliverable)
                .WithMany(entity => entity.Approvals)
                .HasForeignKey(entity => entity.CampaignDeliverableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => new { entity.CampaignDeliverableId, entity.ApprovalType })
                .IsUnique();
        }
    }
}
