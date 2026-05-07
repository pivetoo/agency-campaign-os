using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalBlockConfiguration : IEntityTypeConfiguration<ProposalBlock>
    {
        public void Configure(EntityTypeBuilder<ProposalBlock> builder)
        {
            builder.ToTable("proposalblock");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Body)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(entity => entity.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(255);

            builder.HasIndex(entity => new { entity.Category, entity.Name })
                .HasDatabaseName("ixproposalblockcategoryname");
        }
    }
}
