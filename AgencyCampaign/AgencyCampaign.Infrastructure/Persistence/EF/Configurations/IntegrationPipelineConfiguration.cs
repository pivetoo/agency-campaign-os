using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationPipelineConfiguration : IEntityTypeConfiguration<IntegrationPipeline>
    {
        public void Configure(EntityTypeBuilder<IntegrationPipeline> builder)
        {
            builder.ToTable("integrationpipeline");

            builder.Property(entity => entity.Identifier)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.HasOne(entity => entity.Integration)
                .WithMany(integration => integration.Pipelines)
                .HasForeignKey(entity => entity.IntegrationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => entity.Identifier)
                .IsUnique();

            builder.HasIndex(entity => entity.IntegrationId);
        }
    }
}
