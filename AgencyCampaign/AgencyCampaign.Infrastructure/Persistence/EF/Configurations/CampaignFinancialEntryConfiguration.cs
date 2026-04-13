using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CampaignFinancialEntryConfiguration : IEntityTypeConfiguration<CampaignFinancialEntry>
    {
        public void Configure(EntityTypeBuilder<CampaignFinancialEntry> builder)
        {
            builder.ToTable("campaign_financial_entry");

            builder.Property(entity => entity.Description)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.CounterpartyName)
                .HasMaxLength(150);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.Amount)
                .HasPrecision(18, 2);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.CampaignDeliverable)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignDeliverableId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
