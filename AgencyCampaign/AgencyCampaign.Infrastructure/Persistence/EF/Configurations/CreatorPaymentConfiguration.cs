using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorPaymentConfiguration : IEntityTypeConfiguration<CreatorPayment>
    {
        public void Configure(EntityTypeBuilder<CreatorPayment> builder)
        {
            builder.ToTable("creatorpayment");

            builder.Property(entity => entity.GrossAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(entity => entity.Discounts)
                .HasColumnType("decimal(18,2)");

            builder.Property(entity => entity.NetAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Provider)
                .HasMaxLength(50);

            builder.Property(entity => entity.ProviderTransactionId)
                .HasMaxLength(150);

            builder.Property(entity => entity.PixKey)
                .HasMaxLength(150);

            builder.Property(entity => entity.InvoiceNumber)
                .HasMaxLength(50);

            builder.Property(entity => entity.InvoiceUrl)
                .HasMaxLength(1000);

            builder.Property(entity => entity.FailureReason)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.CampaignCreator)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignCreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.CampaignDocument)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignDocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(entity => entity.Events)
                .WithOne(entity => entity.CreatorPayment)
                .HasForeignKey(entity => entity.CreatorPaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(CreatorPayment.Events))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
