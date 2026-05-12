using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class ProposalTemplateVersionConfiguration : IEntityTypeConfiguration<ProposalTemplateVersion>
    {
        public void Configure(EntityTypeBuilder<ProposalTemplateVersion> builder)
        {
            builder.ToTable("proposaltemplateversion");

            builder.Property(entity => entity.Name).IsRequired().HasMaxLength(100);
            builder.Property(entity => entity.Template).IsRequired().HasColumnType("text");
            builder.Property(entity => entity.IsActive).IsRequired();
        }
    }
}
