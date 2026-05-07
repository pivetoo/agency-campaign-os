using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorSocialHandleConfiguration : IEntityTypeConfiguration<CreatorSocialHandle>
    {
        public void Configure(EntityTypeBuilder<CreatorSocialHandle> builder)
        {
            builder.ToTable("creatorsocialhandle");

            builder.Property(entity => entity.Handle).IsRequired().HasMaxLength(120);
            builder.Property(entity => entity.ProfileUrl).HasMaxLength(500);
            builder.Property(entity => entity.EngagementRate).HasColumnType("decimal(5,2)");

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entity => entity.Platform)
                .WithMany()
                .HasForeignKey(entity => entity.PlatformId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => new { entity.CreatorId, entity.PlatformId })
                .IsUnique()
                .HasDatabaseName("uxcreatorsocialhandlecreatoridplatformid");
        }
    }
}
