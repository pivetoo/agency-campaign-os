using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AgencySettingsConfiguration : IEntityTypeConfiguration<AgencySettings>
    {
        public void Configure(EntityTypeBuilder<AgencySettings> builder)
        {
            builder.ToTable("agencysettings");

            builder.Property(entity => entity.AgencyName).IsRequired().HasMaxLength(150);
            builder.Property(entity => entity.TradeName).HasMaxLength(150);
            builder.Property(entity => entity.Document).HasMaxLength(50);
            builder.Property(entity => entity.PrimaryEmail).HasMaxLength(255);
            builder.Property(entity => entity.Phone).HasMaxLength(50);
            builder.Property(entity => entity.Address).HasMaxLength(500);
            builder.Property(entity => entity.LogoUrl).HasMaxLength(500);
            builder.Property(entity => entity.PrimaryColor).HasMaxLength(32);
            builder.Property(entity => entity.ProposalHtmlTemplate).HasColumnType("text");
            builder.Property(entity => entity.WhatsAppConnectorId);
        }
    }
}
