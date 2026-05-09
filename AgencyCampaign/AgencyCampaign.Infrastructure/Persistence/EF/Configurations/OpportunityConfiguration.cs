using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
    {
        public void Configure(EntityTypeBuilder<Opportunity> builder)
        {
            builder.ToTable("opportunity");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.EstimatedValue)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.Probability)
                .HasPrecision(5, 2);

            builder.Property(entity => entity.ContactName)
                .HasMaxLength(150);

            builder.Property(entity => entity.ContactEmail)
                .HasMaxLength(255);

            builder.Property(entity => entity.Notes)
                .HasMaxLength(1000);

            builder.Property(entity => entity.LossReason)
                .HasMaxLength(1000);

            builder.Property(entity => entity.WonNotes)
                .HasMaxLength(1000);

            builder.HasOne(entity => entity.CommercialPipelineStage)
                .WithMany()
                .HasForeignKey(entity => entity.CommercialPipelineStageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(entity => entity.Brand)
                .WithMany()
                .HasForeignKey(entity => entity.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(entity => entity.ResponsibleUserId);

            builder.HasMany(entity => entity.Negotiations)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(entity => entity.FollowUps)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(entity => entity.Proposals)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(entity => entity.StageHistory)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(entity => entity.Comments)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(entity => entity.OpportunitySource)
                .WithMany()
                .HasForeignKey(entity => entity.OpportunitySourceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(entity => entity.TagAssignments)
                .WithOne(entity => entity.Opportunity)
                .HasForeignKey(entity => entity.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
