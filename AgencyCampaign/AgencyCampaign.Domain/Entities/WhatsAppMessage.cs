using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class WhatsAppMessage : Entity
    {
        public long ConversationId { get; private set; }

        public WhatsAppConversation? Conversation { get; private set; }

        public string? ExternalId { get; private set; }

        public WhatsAppMessageDirection Direction { get; private set; }

        public string Content { get; private set; } = string.Empty;

        public DateTimeOffset SentAt { get; private set; }

        public bool IsRead { get; private set; }

        private WhatsAppMessage()
        {
        }

        public WhatsAppMessage(long conversationId, string content, WhatsAppMessageDirection direction, DateTimeOffset sentAt, string? externalId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(conversationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(content);

            ConversationId = conversationId;
            Content = content.Trim();
            Direction = direction;
            SentAt = sentAt.ToUniversalTime();
            ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim();
            IsRead = direction == WhatsAppMessageDirection.Outbound;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
