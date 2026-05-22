using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityWinReasonConfiguration : IEntityTypeConfiguration<OpportunityWinReason>
    {
        public void Configure(EntityTypeBuilder<OpportunityWinReason> builder)
        {
            builder.ToTable("opportunitywinreason");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);
        }
    }

    public sealed class OpportunityLossReasonConfiguration : IEntityTypeConfiguration<OpportunityLossReason>
    {
        public void Configure(EntityTypeBuilder<OpportunityLossReason> builder)
        {
            builder.ToTable("opportunitylossreason");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);
        }
    }
}
