using AgencyCampaign.Application.Models.WhatsApp;

namespace AgencyCampaign.Application.Services
{
    public interface IWhatsAppNotifier
    {
        Task NotifyNewMessage(long conversationId, WhatsAppMessageModel message, CancellationToken cancellationToken = default);

        Task NotifyConversationUpdated(long conversationId, string? preview, int unreadCount, CancellationToken cancellationToken = default);
    }
}
