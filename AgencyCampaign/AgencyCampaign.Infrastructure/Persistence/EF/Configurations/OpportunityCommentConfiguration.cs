using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class OpportunityCommentConfiguration : IEntityTypeConfiguration<OpportunityComment>
    {
        public void Configure(EntityTypeBuilder<OpportunityComment> builder)
        {
            builder.ToTable("opportunitycomment");

            builder.Property(entity => entity.AuthorName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(entity => entity.Body)
                .IsRequired()
                .HasMaxLength(4000);

            builder.HasIndex(entity => new { entity.OpportunityId, entity.CreatedAt })
                .HasDatabaseName("ixopportunitycommentopportunityidcreatedat");
        }
    }
}
