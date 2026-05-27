using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableReviewCommentConfiguration : IEntityTypeConfiguration<DeliverableReviewComment>
    {
        public void Configure(EntityTypeBuilder<DeliverableReviewComment> builder)
        {
            builder.ToTable("deliverablereviewcomment");

            builder.Property(entity => entity.AuthorName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Body)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(entity => entity.CampaignDeliverableId);
        }
    }
}
