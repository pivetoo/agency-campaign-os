using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class CreatorAccessTokenConfiguration : IEntityTypeConfiguration<CreatorAccessToken>
    {
        public void Configure(EntityTypeBuilder<CreatorAccessToken> builder)
        {
            builder.ToTable("creatoraccesstoken");

            builder.Property(entity => entity.Token)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(entity => entity.Note)
                .HasMaxLength(500);

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(150);

            builder.HasIndex(entity => entity.Token)
                .IsUnique()
                .HasDatabaseName("ixcreatoraccesstokentoken");

            builder.HasOne(entity => entity.Creator)
                .WithMany()
                .HasForeignKey(entity => entity.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
