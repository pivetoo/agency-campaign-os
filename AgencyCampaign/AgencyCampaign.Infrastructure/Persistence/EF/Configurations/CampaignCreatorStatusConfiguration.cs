using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignCreatorStatusConfiguration : IEntityTypeConfiguration<CampaignCreatorStatus>
    {
        public void Configure(EntityTypeBuilder<CampaignCreatorStatus> builder)
        {
            builder.ToTable("campaigncreatorstatus");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);
        }
    }
}
