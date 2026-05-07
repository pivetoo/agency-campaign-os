using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CommercialPipelineStageConfiguration : IEntityTypeConfiguration<CommercialPipelineStage>
    {
        public void Configure(EntityTypeBuilder<CommercialPipelineStage> builder)
        {
            builder.ToTable("commercialpipelinestage");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Color)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(entity => entity.DefaultProbability)
                .HasPrecision(5, 2);
        }
    }
}
