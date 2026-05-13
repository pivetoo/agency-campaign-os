using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgencyCampaign.Infrastructure.Persistence.EF.Configurations
{
    public sealed class WhatsAppConversationConfiguration : IEntityTypeConfiguration<WhatsAppConversation>
    {
        public void Configure(EntityTypeBuilder<WhatsAppConversation> builder)
        {
            builder.ToTable("whatsappconversation");

            builder.Property(e => e.ContactPhone).IsRequired().HasMaxLength(50);
            builder.Property(e => e.ContactName).HasMaxLength(200);
            builder.Property(e => e.LastMessagePreview).HasMaxLength(200);

            builder.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId);
        }
    }
}
