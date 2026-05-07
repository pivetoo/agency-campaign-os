using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
    {
        public void Configure(EntityTypeBuilder<EmailTemplate> builder)
        {
            builder.ToTable("emailtemplate");

            builder.Property(entity => entity.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(entity => entity.EventType)
                .HasConversion<int>();

            builder.Property(entity => entity.Subject)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(entity => entity.HtmlBody)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(entity => entity.CreatedByUserName)
                .HasMaxLength(255);

            builder.HasIndex(entity => new { entity.EventType, entity.IsActive })
                .HasDatabaseName("ixemailtemplateeventtypeisactive");
        }
    }
}
