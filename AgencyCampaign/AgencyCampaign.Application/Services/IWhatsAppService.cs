using AgencyCampaign.Application.Models.WhatsApp;
using AgencyCampaign.Application.Requests.WhatsApp;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IWhatsAppService
    {
        Task<PagedResult<WhatsAppConversationModel>> GetConversations(PagedRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<WhatsAppMessageModel>> GetMessages(long conversationId, CancellationToken cancellationToken = default);

        Task<WhatsAppConversationModel> ReceiveInboundMessage(ReceiveWhatsAppWebhookRequest request, long? connectorId, CancellationToken cancellationToken = default);

        Task<WhatsAppMessageModel> SendMessage(long conversationId, SendWhatsAppMessageRequest request, CancellationToken cancellationToken = default);

        Task MarkAsRead(long conversationId, CancellationToken cancellationToken = default);
    }
}
