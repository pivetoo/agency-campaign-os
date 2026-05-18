using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
    {
        public void Configure(EntityTypeBuilder<BankTransaction> builder)
        {
            builder.ToTable("banktransaction");

            builder.Property(entity => entity.ExternalId).IsRequired().HasMaxLength(200);
            builder.Property(entity => entity.Description).IsRequired().HasMaxLength(500);
            builder.Property(entity => entity.Category).HasMaxLength(200);
            builder.Property(entity => entity.Amount).HasPrecision(18, 2);
            builder.Property(entity => entity.Direction).HasConversion<int>().IsRequired();
            builder.Property(entity => entity.MatchKind).HasConversion<int?>();

            builder.HasIndex(entity => new { entity.AccountId, entity.ExternalId }).IsUnique();
            builder.HasIndex(entity => entity.FinancialEntryId);
            builder.HasIndex(entity => entity.OccurredAt);
        }
    }
}
