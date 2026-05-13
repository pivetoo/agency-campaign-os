using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.WhatsApp;
using AgencyCampaign.Application.Requests.WhatsApp;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class WhatsAppService : IWhatsAppService
    {
        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient integrationClient;
        private readonly IWhatsAppNotifier notifier;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ILogger<WhatsAppService> logger;

        public WhatsAppService(DbContext dbContext, IntegrationPlatformClient integrationClient, IWhatsAppNotifier notifier, IStringLocalizer<AgencyCampaignResource> localizer, ILogger<WhatsAppService> logger)
        {
            this.dbContext = dbContext;
            this.integrationClient = integrationClient;
            this.notifier = notifier;
            this.localizer = localizer;
            this.logger = logger;
        }

        public async Task<PagedResult<WhatsAppConversationModel>> GetConversations(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<WhatsAppConversation>()
                .AsNoTracking()
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToPagedResultAsync(request, MapConversation, cancellationToken);
        }

        public async Task<IReadOnlyList<WhatsAppMessageModel>> GetMessages(long conversationId, CancellationToken cancellationToken = default)
        {
            List<WhatsAppMessage> messages = await dbContext.Set<WhatsAppMessage>()
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            return messages.Select(MapMessage).ToList();
        }

        public async Task<WhatsAppConversationModel> ReceiveInboundMessage(ReceiveWhatsAppWebhookRequest request, long? connectorId, CancellationToken cancellationToken = default)
        {
            WhatsAppConversation? conversation = await dbContext.Set<WhatsAppConversation>()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.ContactPhone == request.From, cancellationToken);

            if (conversation is null)
            {
                conversation = new WhatsAppConversation(request.From, connectorId);
                dbContext.Set<WhatsAppConversation>().Add(conversation);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            DateTimeOffset sentAt = request.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(request.Timestamp)
                : DateTimeOffset.UtcNow;

            WhatsAppMessage message = new(conversation.Id, request.Message, WhatsAppMessageDirection.Inbound, sentAt);
            dbContext.Set<WhatsAppMessage>().Add(message);

            conversation.RegisterInboundMessage(request.Message, sentAt);

            await dbContext.SaveChangesAsync(cancellationToken);

            WhatsAppMessageModel messageModel = MapMessage(message);
            WhatsAppConversationModel conversationModel = MapConversation(conversation);

            await notifier.NotifyNewMessage(conversation.Id, messageModel, cancellationToken);
            await notifier.NotifyConversationUpdated(conversation.Id, conversation.LastMessagePreview, conversation.UnreadCount, cancellationToken);

            return conversationModel;
        }

        public async Task<WhatsAppMessageModel> SendMessage(long conversationId, SendWhatsAppMessageRequest request, CancellationToken cancellationToken = default)
        {
            WhatsAppConversation? conversation = await dbContext.Set<WhatsAppConversation>()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            WhatsAppMessage message = new(conversation.Id, request.Message, WhatsAppMessageDirection.Outbound, now);
            dbContext.Set<WhatsAppMessage>().Add(message);

            conversation.RegisterOutboundMessage(request.Message, now);

            await dbContext.SaveChangesAsync(cancellationToken);

            _ = TrySendViaIntegrationPlatformAsync(conversation, message.Id, request.Message, cancellationToken);

            return MapMessage(message);
        }

        public async Task MarkAsRead(long conversationId, CancellationToken cancellationToken = default)
        {
            WhatsAppConversation? conversation = await dbContext.Set<WhatsAppConversation>()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation is null)
            {
                return;
            }

            conversation.MarkAsRead();

            List<WhatsAppMessage> unread = await dbContext.Set<WhatsAppMessage>()
                .AsTracking()
                .Where(m => m.ConversationId == conversationId && !m.IsRead)
                .ToListAsync(cancellationToken);

            foreach (WhatsAppMessage msg in unread)
            {
                msg.MarkAsRead();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task TrySendViaIntegrationPlatformAsync(WhatsAppConversation conversation, long messageId, string message, CancellationToken cancellationToken)
        {
            if (conversation.ConnectorId is null)
            {
                const string noConnectorError = "Nenhum conector WhatsApp configurado para esta conversa.";
                logger.LogWarning("Outbound message {MessageId} cannot be sent: conversation {ConversationId} has no connectorId.", messageId, conversation.Id);
                await notifier.NotifyMessageSendFailed(conversation.Id, messageId, noConnectorError, cancellationToken);
                return;
            }

            try
            {
                ConnectorDto connector = await integrationClient.GetConnectorByIdAsync(conversation.ConnectorId.Value, cancellationToken);
                List<PipelineDto> pipelines = await integrationClient.GetPipelinesByIntegrationAsync(connector.IntegrationId, cancellationToken);

                PipelineDto? sendPipeline = pipelines.FirstOrDefault(p => p.Identifier.EndsWith("-send", StringComparison.OrdinalIgnoreCase));
                if (sendPipeline is null)
                {
                    const string noPipelineError = "Pipeline de envio não encontrado para esta integração.";
                    logger.LogWarning("Outbound message {MessageId}: no '-send' pipeline found for integration {IntegrationId}.", messageId, connector.IntegrationId);
                    await notifier.NotifyMessageSendFailed(conversation.Id, messageId, noPipelineError, cancellationToken);
                    return;
                }

                string payload = JsonSerializer.Serialize(new { to = conversation.ContactPhone, message });

                ExecutePipelineRequest executeRequest = new()
                {
                    ConnectorId = conversation.ConnectorId.Value,
                    PipelineId = sendPipeline.Id,
                    InputData = payload
                };

                await integrationClient.ExecutePipelineAsync(executeRequest, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send outbound message {MessageId} via IntegrationPlatform.", messageId);
                await notifier.NotifyMessageSendFailed(conversation.Id, messageId, "Falha ao enviar a mensagem. Tente novamente.", cancellationToken);
            }
        }

        private static WhatsAppConversationModel MapConversation(WhatsAppConversation c) => new()
        {
            Id = c.Id,
            ConnectorId = c.ConnectorId,
            ContactPhone = c.ContactPhone,
            ContactName = c.ContactName,
            LastMessageAt = c.LastMessageAt,
            LastMessagePreview = c.LastMessagePreview,
            UnreadCount = c.UnreadCount,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
        };

        private static WhatsAppMessageModel MapMessage(WhatsAppMessage m) => new()
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            ExternalId = m.ExternalId,
            Direction = m.Direction,
            Content = m.Content,
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt,
        };
    }
}
