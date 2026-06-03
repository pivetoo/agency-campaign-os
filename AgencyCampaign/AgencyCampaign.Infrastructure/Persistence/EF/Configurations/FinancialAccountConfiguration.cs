using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
    {
        public void Configure(EntityTypeBuilder<FinancialAccount> builder)
        {
            builder.ToTable("financialaccount");

            // Concorrencia otimista (D5i): protege writes monetarios/de revisao concorrentes.
            builder.UseXminConcurrencyToken();

            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(120);
            builder.Property(entity => entity.BankId);
            builder.Property(entity => entity.Bank).HasMaxLength(120);
            builder.HasIndex(entity => entity.BankId);
            builder.Property(entity => entity.Agency).HasMaxLength(50);
            builder.Property(entity => entity.Number).HasMaxLength(50);
            builder.Property(entity => entity.Color).IsRequired().HasMaxLength(32);
            builder.Property(entity => entity.InitialBalance).HasPrecision(18, 2);
            builder.Property(entity => entity.IsActive).IsRequired();
            builder.Property(entity => entity.LastSyncedBalance).HasPrecision(18, 2);
            builder.Property(entity => entity.SyncStatus)
                .HasConversion<int>()
                .HasDefaultValue(FinancialAccountSyncStatus.NotConfigured)
                .IsRequired();
        }
    }
}
