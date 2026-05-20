using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationCapabilityConfiguration : IEntityTypeConfiguration<IntegrationCapability>
    {
        public void Configure(EntityTypeBuilder<IntegrationCapability> builder)
        {
            builder.ToTable("integrationcapability");

            builder.Property(entity => entity.IntentKey)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.ConnectorId)
                .IsRequired();

            builder.Property(entity => entity.IsActive)
                .IsRequired();

            builder.HasIndex(entity => entity.IntentKey).IsUnique();
        }
    }
}
