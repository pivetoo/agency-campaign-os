using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
    {
        public void Configure(EntityTypeBuilder<FinancialAccount> builder)
        {
            builder.ToTable("financialaccount");

            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(120);
            builder.Property(entity => entity.Bank).HasMaxLength(120);
            builder.Property(entity => entity.Agency).HasMaxLength(50);
            builder.Property(entity => entity.Number).HasMaxLength(50);
            builder.Property(entity => entity.Color).IsRequired().HasMaxLength(32);
            builder.Property(entity => entity.InitialBalance).HasPrecision(18, 2);
        }
    }
}
