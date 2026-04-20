using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityNegotiationConfiguration : IEntityTypeConfiguration<OpportunityNegotiation>
    {
        public void Configure(EntityTypeBuilder<OpportunityNegotiation> builder)
        {
            builder.ToTable("opportunitynegotiation");

            builder.Property(entity => entity.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Amount)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.Opportunity)
                .WithMany(entity => entity.Negotiations)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
