using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignCreatorConfiguration : IEntityTypeConfiguration<CampaignCreator>
    {
        public void Configure(EntityTypeBuilder<CampaignCreator> builder)
        {
            builder.ToTable("campaigncreator");

            builder.Property(entity => entity.AgreedAmount)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.AgencyFeePercent)
                .HasPrecision(5, 2);

            builder.Property(entity => entity.AgencyFeeAmount)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.Campaign)
                .WithMany(entity => entity.CampaignCreators)
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.CampaignCreatorStatus)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignCreatorStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => new { entity.CampaignId, entity.CreatorId })
                .IsUnique();
        }
    }
}
