using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class FinancialPeriodConfiguration : IEntityTypeConfiguration<FinancialPeriod>
    {
        public void Configure(EntityTypeBuilder<FinancialPeriod> builder)
        {
            builder.ToTable("financialperiod");

            builder.UseXminConcurrencyToken();

            builder.Property(entity => entity.Year).IsRequired();
            builder.Property(entity => entity.Month).IsRequired();
            builder.Property(entity => entity.IsClosed).IsRequired();

            builder.HasIndex(entity => new { entity.Year, entity.Month }).IsUnique();
        }
    }
}
