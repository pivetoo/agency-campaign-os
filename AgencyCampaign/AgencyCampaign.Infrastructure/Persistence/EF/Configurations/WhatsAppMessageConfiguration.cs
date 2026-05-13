using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class WhatsAppMessageConfiguration : IEntityTypeConfiguration<WhatsAppMessage>
    {
        public void Configure(EntityTypeBuilder<WhatsAppMessage> builder)
        {
            builder.ToTable("whatsappmessage");

            builder.Property(e => e.ExternalId).HasMaxLength(200);
            builder.Property(e => e.Content).HasColumnType("text").IsRequired();
            builder.Property(e => e.Direction).HasConversion<int>();
        }
    }
}
