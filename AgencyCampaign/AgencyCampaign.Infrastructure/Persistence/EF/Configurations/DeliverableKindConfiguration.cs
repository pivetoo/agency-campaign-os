using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class DeliverableKindConfiguration : IEntityTypeConfiguration<DeliverableKind>
    {
        public void Configure(EntityTypeBuilder<DeliverableKind> builder)
        {
            builder.ToTable("deliverablekind");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(120);
        }
    }
}
