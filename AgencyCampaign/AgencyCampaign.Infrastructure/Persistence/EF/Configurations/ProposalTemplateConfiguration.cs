using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalTemplateConfiguration : IEntityTypeConfiguration<ProposalTemplate>
    {
        public void Configure(EntityTypeBuilder<ProposalTemplate> builder)
        {
            builder.ToTable("proposaltemplate");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(255);

            builder.HasMany(entity => entity.Items)
                .WithOne(item => item.ProposalTemplate)
                .HasForeignKey(item => item.ProposalTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class ProposalTemplateItemConfiguration : IEntityTypeConfiguration<ProposalTemplateItem>
    {
        public void Configure(EntityTypeBuilder<ProposalTemplateItem> builder)
        {
            builder.ToTable("proposaltemplateitem");

            builder.Property(entity => entity.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(entity => entity.DefaultUnitPrice)
                .HasPrecision(18, 2);

            builder.Property(entity => entity.Observations)
                .HasMaxLength(1000);

            builder.HasIndex(entity => new { entity.ProposalTemplateId, entity.DisplayOrder })
                .HasDatabaseName("ixproposaltemplateitemproposaltemplateiddisplayorder");
        }
    }
}
