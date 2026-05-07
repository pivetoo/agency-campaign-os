using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class FinancialSubcategoryConfiguration : IEntityTypeConfiguration<FinancialSubcategory>
    {
        public void Configure(EntityTypeBuilder<FinancialSubcategory> builder)
        {
            builder.ToTable("financialsubcategory");

            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(120);
            builder.Property(entity => entity.Color).IsRequired().HasMaxLength(32);

            builder.HasIndex(entity => entity.MacroCategory)
                .HasDatabaseName("ixfinancialsubcategorymacrocategory");
        }
    }
}
