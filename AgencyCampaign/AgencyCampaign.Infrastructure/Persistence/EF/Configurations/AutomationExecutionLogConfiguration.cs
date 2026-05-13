using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class AutomationExecutionLogConfiguration : IEntityTypeConfiguration<AutomationExecutionLog>
    {
        public void Configure(EntityTypeBuilder<AutomationExecutionLog> builder)
        {
            builder.ToTable("automationexecutionlog");

            builder.Property(entity => entity.AutomationName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Trigger)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.RenderedPayload)
                .HasColumnType("text");

            builder.Property(entity => entity.ErrorMessage)
                .HasMaxLength(2000);

            builder.HasOne(entity => entity.Automation)
                .WithMany()
                .HasForeignKey(entity => entity.AutomationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
