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
using System.Text.Json;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class WhatsAppService : IWhatsAppService
    {
        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient integrationClient;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public WhatsAppService(DbContext dbContext, IntegrationPlatformClient integrationClient, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.integrationClient = integrationClient;
            this.localizer = localizer;
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

            return MapConversation(conversation);
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

            _ = TrySendViaIntegrationPlatformAsync(conversation, request.Message, cancellationToken);

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

        private async Task TrySendViaIntegrationPlatformAsync(WhatsAppConversation conversation, string message, CancellationToken cancellationToken)
        {
            if (conversation.ConnectorId is null)
            {
                Console.WriteLine("[WhatsAppService] No connectorId on conversation — cannot send via IntegrationPlatform.");
                return;
            }

            try
            {
                ConnectorDto connector = await integrationClient.GetConnectorByIdAsync(conversation.ConnectorId.Value, cancellationToken);
                List<PipelineDto> pipelines = await integrationClient.GetPipelinesByIntegrationAsync(connector.IntegrationId, cancellationToken);

                PipelineDto? sendPipeline = pipelines.FirstOrDefault(p => p.Identifier.EndsWith("-send", StringComparison.OrdinalIgnoreCase));
                if (sendPipeline is null)
                {
                    Console.WriteLine($"[WhatsAppService] No send pipeline found for integration {connector.IntegrationId}.");
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
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsAppService] Failed to send message via IntegrationPlatform: {ex.Message}");
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
