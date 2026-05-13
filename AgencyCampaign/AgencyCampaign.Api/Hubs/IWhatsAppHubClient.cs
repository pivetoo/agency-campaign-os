using AgencyCampaign.Application.Models.WhatsApp;

namespace AgencyCampaign.Api.Hubs
{
    public interface IWhatsAppHubClient
    {
        Task NewMessage(long conversationId, WhatsAppMessageModel message);

        Task ConversationUpdated(long conversationId, string? preview, int unreadCount);
    }
}
