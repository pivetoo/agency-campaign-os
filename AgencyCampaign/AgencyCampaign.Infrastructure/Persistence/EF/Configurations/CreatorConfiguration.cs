using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorConfiguration : IEntityTypeConfiguration<Creator>
    {
        public void Configure(EntityTypeBuilder<Creator> builder)
        {
            builder.ToTable("creator");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.StageName)
                .HasMaxLength(150);

            builder.Property(entity => entity.Email)
                .HasMaxLength(150);

            builder.Property(entity => entity.Phone)
                .HasMaxLength(50);

            builder.Property(entity => entity.Document)
                .HasMaxLength(30);

            builder.Property(entity => entity.PixKey)
                .HasMaxLength(150);

            builder.Property(entity => entity.PrimaryNiche)
                .HasMaxLength(120);

            builder.Property(entity => entity.City)
                .HasMaxLength(120);

            builder.Property(entity => entity.State)
                .HasMaxLength(50);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.DefaultAgencyFeePercent)
                .HasPrecision(5, 2);

            builder.Property(entity => entity.DefaultAgencyFeePercent)
                .HasDefaultValue(0);
        }
    }
}
