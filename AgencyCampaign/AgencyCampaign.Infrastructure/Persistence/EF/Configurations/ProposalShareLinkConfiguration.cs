using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalShareLinkConfiguration : IEntityTypeConfiguration<ProposalShareLink>
    {
        public void Configure(EntityTypeBuilder<ProposalShareLink> builder)
        {
            builder.ToTable("proposalsharelink");

            builder.Property(entity => entity.Token)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(255);

            builder.HasIndex(entity => entity.Token)
                .IsUnique()
                .HasDatabaseName("ixproposalsharelinktoken");

            builder.HasMany(entity => entity.Views)
                .WithOne(view => view.ProposalShareLink)
                .HasForeignKey(view => view.ProposalShareLinkId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
