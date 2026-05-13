using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class WhatsAppConversation : Entity
    {
        private readonly List<WhatsAppMessage> messages = [];

        public long? ConnectorId { get; private set; }

        public string ContactPhone { get; private set; } = string.Empty;

        public string? ContactName { get; private set; }

        public DateTimeOffset? LastMessageAt { get; private set; }

        public string? LastMessagePreview { get; private set; }

        public int UnreadCount { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<WhatsAppMessage> Messages => messages.AsReadOnly();

        private WhatsAppConversation()
        {
        }

        public WhatsAppConversation(string contactPhone, long? connectorId = null, string? contactName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contactPhone);

            ContactPhone = contactPhone.Trim();
            ConnectorId = connectorId;
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
        }

        public void RegisterInboundMessage(string content, DateTimeOffset sentAt)
        {
            LastMessageAt = sentAt;
            LastMessagePreview = content.Length > 100 ? content[..100] : content;
            UnreadCount++;
        }

        public void RegisterOutboundMessage(string content, DateTimeOffset sentAt)
        {
            LastMessageAt = sentAt;
            LastMessagePreview = content.Length > 100 ? content[..100] : content;
        }

        public void MarkAsRead()
        {
            UnreadCount = 0;
        }

        public void UpdateContactName(string? name)
        {
            ContactName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }
    }
}
