using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignDocumentEventConfiguration : IEntityTypeConfiguration<CampaignDocumentEvent>
    {
        public void Configure(EntityTypeBuilder<CampaignDocumentEvent> builder)
        {
            builder.ToTable("campaigndocumentevent");

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Metadata)
                .HasColumnType("text");
        }
    }
}
