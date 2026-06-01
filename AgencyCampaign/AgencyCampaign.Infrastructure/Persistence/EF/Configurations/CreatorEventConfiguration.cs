using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorEventConfiguration : IEntityTypeConfiguration<CreatorEvent>
    {
        public void Configure(EntityTypeBuilder<CreatorEvent> builder)
        {
            builder.ToTable("creatorevent");

            builder.HasIndex(entity => entity.CreatorId);

            builder.Property(entity => entity.Description)
                .HasMaxLength(500);

            builder.Property(entity => entity.Metadata)
                .HasColumnType("text");
        }
    }
}
