using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignDocumentConfiguration : IEntityTypeConfiguration<CampaignDocument>
    {
        public void Configure(EntityTypeBuilder<CampaignDocument> builder)
        {
            builder.ToTable("campaigndocument");

            builder.Property(entity => entity.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.DocumentUrl)
                .HasMaxLength(1000);

            builder.Property(entity => entity.RecipientEmail)
                .HasMaxLength(150);

            builder.Property(entity => entity.EmailSubject)
                .HasMaxLength(200);

            builder.Property(entity => entity.EmailBody)
                .HasMaxLength(5000);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.CampaignCreator)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignCreatorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
