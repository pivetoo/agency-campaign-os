using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorSocialHandleSnapshotConfiguration : IEntityTypeConfiguration<CreatorSocialHandleSnapshot>
    {
        public void Configure(EntityTypeBuilder<CreatorSocialHandleSnapshot> builder)
        {
            builder.ToTable("creatorsocialhandlesnapshot");

            builder.Property(entity => entity.Source).IsRequired().HasMaxLength(50);
            builder.Property(entity => entity.EngagementRate).HasPrecision(5, 2);

            builder.HasOne(entity => entity.CreatorSocialHandle)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorSocialHandleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.CreatorSocialHandleId, entity.Year, entity.Month })
                .IsUnique()
                .HasDatabaseName("uxcreatorsocialhandlesnapshotperiod");
        }
    }
}
