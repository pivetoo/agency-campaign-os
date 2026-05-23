using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CommercialPolicyConfiguration : IEntityTypeConfiguration<CommercialPolicy>
    {
        public void Configure(EntityTypeBuilder<CommercialPolicy> builder)
        {
            builder.ToTable("commercialpolicy");

            builder.Property(entity => entity.MaxDiscountPercent).HasPrecision(5, 2);
            builder.Property(entity => entity.MinMarginPercent).HasPrecision(5, 2);
            builder.Property(entity => entity.DefaultPaymentTermDays);
            builder.Property(entity => entity.MaxPaymentTermDays);
            builder.Property(entity => entity.Notes).HasMaxLength(1000);
        }
    }
}
