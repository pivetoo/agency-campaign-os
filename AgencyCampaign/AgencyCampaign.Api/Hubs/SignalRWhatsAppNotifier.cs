using AgencyCampaign.Application.Models.WhatsApp;
using AgencyCampaign.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace AgencyCampaign.Api.Hubs
{
    public sealed class SignalRWhatsAppNotifier : IWhatsAppNotifier
    {
        private readonly IHubContext<WhatsAppHub, IWhatsAppHubClient> hubContext;

        public SignalRWhatsAppNotifier(IHubContext<WhatsAppHub, IWhatsAppHubClient> hubContext)
        {
            this.hubContext = hubContext;
        }

        public Task NotifyNewMessage(long conversationId, WhatsAppMessageModel message, CancellationToken cancellationToken = default)
        {
            return hubContext.Clients.All.NewMessage(conversationId, message);
        }

        public Task NotifyConversationUpdated(long conversationId, string? preview, int unreadCount, CancellationToken cancellationToken = default)
        {
            return hubContext.Clients.All.ConversationUpdated(conversationId, preview, unreadCount);
        }

        public Task NotifyMessageSendFailed(long conversationId, long messageId, string error, CancellationToken cancellationToken = default)
        {
            return hubContext.Clients.All.MessageSendFailed(conversationId, messageId, error);
        }
    }
}
