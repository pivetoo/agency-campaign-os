using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class FinancialEntryConfiguration : IEntityTypeConfiguration<FinancialEntry>
    {
        public void Configure(EntityTypeBuilder<FinancialEntry> builder)
        {
            builder.ToTable("financialentry");

            builder.Property(entity => entity.Description)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.PaymentMethod)
                .HasMaxLength(100);

            builder.Property(entity => entity.ReferenceCode)
                .HasMaxLength(100);

            builder.Property(entity => entity.CounterpartyName)
                .HasMaxLength(150);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.Amount)
                .HasPrecision(18, 2);

            builder.HasOne(entity => entity.Account)
                .WithMany()
                .HasForeignKey(entity => entity.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.CampaignDeliverable)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignDeliverableId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(entity => new { entity.AccountId, entity.DueAt })
                .HasDatabaseName("ixfinancialentryaccountiddueat");
        }
    }
}
