namespace AgencyCampaign.Application.Models.WhatsApp
{
    public sealed class WhatsAppConversationModel
    {
        public long Id { get; set; }
        public long? ConnectorId { get; set; }
        public string ContactPhone { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public DateTimeOffset? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
