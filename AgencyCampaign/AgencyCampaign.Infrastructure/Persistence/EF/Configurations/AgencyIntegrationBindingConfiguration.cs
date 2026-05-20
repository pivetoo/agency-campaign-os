using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AgencyIntegrationBindingConfiguration : IEntityTypeConfiguration<AgencyIntegrationBinding>
    {
        public void Configure(EntityTypeBuilder<AgencyIntegrationBinding> builder)
        {
            builder.ToTable("agencyintegrationbinding");

            builder.Property(entity => entity.IntentKey).IsRequired().HasMaxLength(80);
            builder.Property(entity => entity.ConnectorId).IsRequired();
            builder.Property(entity => entity.PipelineId).IsRequired();
            builder.Property(entity => entity.IsActive).IsRequired();
            builder.Property(entity => entity.CreatedByUserName).HasMaxLength(150);

            builder.HasIndex(entity => entity.IntentKey).IsUnique();
        }
    }
}
