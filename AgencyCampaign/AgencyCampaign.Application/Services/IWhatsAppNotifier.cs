using AgencyCampaign.Application.Models.WhatsApp;

namespace AgencyCampaign.Application.Services
{
    public interface IWhatsAppNotifier
    {
        Task NotifyNewMessage(long conversationId, WhatsAppMessageModel message, CancellationToken cancellationToken = default);

        Task NotifyConversationUpdated(long conversationId, string? preview, int unreadCount, CancellationToken cancellationToken = default);

        Task NotifyMessageSendFailed(long conversationId, long messageId, string error, CancellationToken cancellationToken = default);
    }
}
