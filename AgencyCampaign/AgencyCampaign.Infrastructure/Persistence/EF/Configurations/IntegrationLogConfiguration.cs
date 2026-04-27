using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
    {
        public void Configure(EntityTypeBuilder<IntegrationLog> builder)
        {
            builder.ToTable("integrationlog");

            builder.Property(entity => entity.Payload)
                .HasMaxLength(4000);

            builder.Property(entity => entity.Response)
                .HasMaxLength(4000);

            builder.Property(entity => entity.ErrorMessage)
                .HasMaxLength(2000);

            builder.HasOne(entity => entity.IntegrationPipeline)
                .WithMany()
                .HasForeignKey(entity => entity.IntegrationPipelineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(entity => entity.IntegrationPipelineId);
            builder.HasIndex(entity => entity.CreatedAt);
        }
    }
}
