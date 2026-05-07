using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableShareLinkConfiguration : IEntityTypeConfiguration<DeliverableShareLink>
    {
        public void Configure(EntityTypeBuilder<DeliverableShareLink> builder)
        {
            builder.ToTable("deliverablesharelink");

            builder.Property(entity => entity.Token).IsRequired().HasMaxLength(80);
            builder.Property(entity => entity.ReviewerName).IsRequired().HasMaxLength(150);
            builder.Property(entity => entity.CreatedByUserName).HasMaxLength(150);

            builder.HasOne(entity => entity.CampaignDeliverable)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignDeliverableId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.Token)
                .IsUnique()
                .HasDatabaseName("uxdeliverablesharelinktoken");

            builder.HasIndex(entity => entity.CampaignDeliverableId)
                .HasDatabaseName("ixdeliverablesharelinkcampaigndeliverableid");
        }
    }
}
