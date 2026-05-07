using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalVersionConfiguration : IEntityTypeConfiguration<ProposalVersion>
    {
        public void Configure(EntityTypeBuilder<ProposalVersion> builder)
        {
            builder.ToTable("proposalversion");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.TotalValue)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.SnapshotJson)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(entity => entity.SentByUserName)
                .HasMaxLength(255);

            builder.HasIndex(entity => new { entity.ProposalId, entity.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ixproposalversionproposalidversionnumber");
        }
    }
}
