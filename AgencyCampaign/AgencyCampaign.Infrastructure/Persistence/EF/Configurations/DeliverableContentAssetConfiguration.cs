using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableContentAssetConfiguration : IEntityTypeConfiguration<DeliverableContentAsset>
    {
        public void Configure(EntityTypeBuilder<DeliverableContentAsset> builder)
        {
            builder.ToTable("deliverablecontentasset");

            builder.Property(entity => entity.Url)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(entity => entity.FileName)
                .HasMaxLength(300);

            builder.Property(entity => entity.ContentType)
                .HasMaxLength(120);
        }
    }
}
