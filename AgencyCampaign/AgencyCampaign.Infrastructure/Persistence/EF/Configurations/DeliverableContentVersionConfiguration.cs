using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableContentVersionConfiguration : IEntityTypeConfiguration<DeliverableContentVersion>
    {
        public void Configure(EntityTypeBuilder<DeliverableContentVersion> builder)
        {
            builder.ToTable("deliverablecontentversion");

            // Concorrencia otimista (D5i): protege writes monetarios/de revisao concorrentes.
            builder.UseXminConcurrencyToken();

            builder.Property(entity => entity.SubmittedByName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(entity => entity.Note)
                .HasMaxLength(2000);

            builder.HasMany(entity => entity.Assets)
                .WithOne()
                .HasForeignKey(asset => asset.DeliverableContentVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.CampaignDeliverableId, entity.RoundNumber })
                .IsUnique();
        }
    }
}
