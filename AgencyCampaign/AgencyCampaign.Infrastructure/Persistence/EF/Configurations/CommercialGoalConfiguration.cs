using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CommercialGoalConfiguration : IEntityTypeConfiguration<CommercialGoal>
    {
        public void Configure(EntityTypeBuilder<CommercialGoal> builder)
        {
            builder.ToTable("commercialgoal");

            builder.Property(entity => entity.UserId);

            builder.Property(entity => entity.PeriodType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(entity => entity.PeriodStart)
                .IsRequired();

            builder.Property(entity => entity.TargetAmount)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.HasIndex(entity => new { entity.UserId, entity.PeriodType, entity.PeriodStart })
                .IsUnique()
                .HasDatabaseName("ixcommercialgoaluserperiod");
        }
    }
}
