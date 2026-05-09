using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignDocumentSignatureConfiguration : IEntityTypeConfiguration<CampaignDocumentSignature>
    {
        public void Configure(EntityTypeBuilder<CampaignDocumentSignature> builder)
        {
            builder.ToTable("campaigndocumentsignature");

            builder.Property(entity => entity.SignerName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.SignerEmail)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.SignerDocumentNumber)
                .HasMaxLength(50);

            builder.Property(entity => entity.ProviderSignerId)
                .HasMaxLength(150);

            builder.Property(entity => entity.IpAddress)
                .HasMaxLength(50);

            builder.Property(entity => entity.UserAgent)
                .HasMaxLength(500);
        }
    }
}
