using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunitySourceConfiguration : IEntityTypeConfiguration<OpportunitySource>
    {
        public void Configure(EntityTypeBuilder<OpportunitySource> builder)
        {
            builder.ToTable("opportunitysource");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);
        }
    }

    public sealed class OpportunityTagConfiguration : IEntityTypeConfiguration<OpportunityTag>
    {
        public void Configure(EntityTypeBuilder<OpportunityTag> builder)
        {
            builder.ToTable("opportunitytag");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);
        }
    }

    public sealed class OpportunityTagAssignmentConfiguration : IEntityTypeConfiguration<OpportunityTagAssignment>
    {
        public void Configure(EntityTypeBuilder<OpportunityTagAssignment> builder)
        {
            builder.ToTable("opportunitytagassignment");

            builder.HasOne(entity => entity.OpportunityTag)
                .WithMany()
                .HasForeignKey(entity => entity.OpportunityTagId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(entity => new { entity.OpportunityId, entity.OpportunityTagId })
                .IsUnique()
                .HasDatabaseName("ixopportunitytagassignmentopportunityidtagid");
        }
    }
}
