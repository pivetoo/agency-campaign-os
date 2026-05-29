using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class BrandContactConfiguration : IEntityTypeConfiguration<BrandContact>
    {
        public void Configure(EntityTypeBuilder<BrandContact> builder)
        {
            builder.ToTable("brandcontact");

            builder.Property(entity => entity.Value).IsRequired().HasMaxLength(255);
            builder.Property(entity => entity.Label).HasMaxLength(100);

            builder.HasOne<Brand>()
                .WithMany()
                .HasForeignKey(entity => entity.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.BrandId);
        }
    }
}
