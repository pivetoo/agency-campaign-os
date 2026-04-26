using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
    {
        public void Configure(EntityTypeBuilder<Proposal> builder)
        {
            builder.ToTable("proposal");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.InternalOwnerName)
                .HasMaxLength(150);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.TotalValue)
                .HasPrecision(18, 2);

            builder.HasOne(entity => entity.Campaign)
                .WithMany()
                .HasForeignKey(entity => entity.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(entity => entity.Opportunity)
                .WithMany(entity => entity.Proposals)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(entity => entity.Items)
                .WithOne(item => item.Proposal)
                .HasForeignKey(item => item.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
