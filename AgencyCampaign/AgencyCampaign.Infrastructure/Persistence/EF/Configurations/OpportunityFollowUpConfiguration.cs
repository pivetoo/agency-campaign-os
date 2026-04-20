using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityFollowUpConfiguration : IEntityTypeConfiguration<OpportunityFollowUp>
    {
        public void Configure(EntityTypeBuilder<OpportunityFollowUp> builder)
        {
            builder.ToTable("opportunityfollowup");

            builder.Property(entity => entity.Subject)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.Opportunity)
                .WithMany(entity => entity.FollowUps)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
