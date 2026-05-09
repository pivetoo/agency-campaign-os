using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorPaymentEventConfiguration : IEntityTypeConfiguration<CreatorPaymentEvent>
    {
        public void Configure(EntityTypeBuilder<CreatorPaymentEvent> builder)
        {
            builder.ToTable("creatorpaymentevent");

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Metadata)
                .HasColumnType("text");
        }
    }
}
