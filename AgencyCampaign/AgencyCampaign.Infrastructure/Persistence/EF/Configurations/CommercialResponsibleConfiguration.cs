using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CommercialResponsibleConfiguration : IEntityTypeConfiguration<CommercialResponsible>
    {
        public void Configure(EntityTypeBuilder<CommercialResponsible> builder)
        {
            builder.ToTable("commercialresponsible");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Email)
                .HasMaxLength(255);

            builder.Property(entity => entity.Phone)
                .HasMaxLength(50);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasIndex(entity => entity.UserId)
                .IsUnique()
                .HasDatabaseName("ixcommercialresponsibleuserid");
        }
    }
}
