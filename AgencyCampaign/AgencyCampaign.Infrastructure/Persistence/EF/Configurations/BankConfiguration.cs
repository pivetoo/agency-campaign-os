using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
    {
        public void Configure(EntityTypeBuilder<Bank> builder)
        {
            builder.ToTable("bank");

            builder.Property(entity => entity.Compe).IsRequired().HasMaxLength(3);
            builder.Property(entity => entity.Ispb).HasMaxLength(8);
            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(160);
            builder.Property(entity => entity.ShortName).IsRequired().HasMaxLength(80);
            builder.Property(entity => entity.LogoUrl).HasMaxLength(500);
            builder.Property(entity => entity.IsActive).IsRequired();
            builder.Property(entity => entity.IsSystem).IsRequired().HasDefaultValue(false);

            builder.HasIndex(entity => entity.Compe).IsUnique();
        }
    }
}
