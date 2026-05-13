using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.WhatsApp
{
    public sealed class WhatsAppMessageModel
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public string? ExternalId { get; set; }
        public WhatsAppMessageDirection Direction { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
