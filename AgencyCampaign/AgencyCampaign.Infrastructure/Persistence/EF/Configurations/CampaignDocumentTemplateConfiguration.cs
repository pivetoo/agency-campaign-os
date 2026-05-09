using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignDocumentTemplateConfiguration : IEntityTypeConfiguration<CampaignDocumentTemplate>
    {
        public void Configure(EntityTypeBuilder<CampaignDocumentTemplate> builder)
        {
            builder.ToTable("campaigndocumenttemplate");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Body)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(150);
        }
    }
}
