using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignBriefingConfiguration : IEntityTypeConfiguration<CampaignBriefing>
    {
        public void Configure(EntityTypeBuilder<CampaignBriefing> builder)
        {
            builder.ToTable("campaignbriefing");

            builder.Property(entity => entity.KeyMessage).HasColumnType("text");
            builder.Property(entity => entity.Dos).HasColumnType("text");
            builder.Property(entity => entity.Donts).HasColumnType("text");
            builder.Property(entity => entity.Hashtags).HasColumnType("text");
            builder.Property(entity => entity.Mentions).HasColumnType("text");
            builder.Property(entity => entity.ReferenceLinks).HasColumnType("text");

            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => entity.CampaignId).IsUnique();
        }
    }
}
