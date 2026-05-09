using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CreatorPaymentService : CrudService<CreatorPayment>, ICreatorPaymentService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public CreatorPaymentService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, IntegrationPlatformClient integrationPlatformClient) : base(dbContext)
        {
            this.localizer = localizer;
            this.integrationPlatformClient = integrationPlatformClient;
        }

        public async Task<PagedResult<CreatorPayment>> GetPayments(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CreatorPayment?> GetPaymentById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CreatorPayment>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignCreator!.CampaignId == campaignId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CreatorPayment>> GetByStatus(int status, CancellationToken cancellationToken = default)
        {
            PaymentStatus typed = (PaymentStatus)status;
            return await QueryWithDetails()
                .Where(item => item.Status == typed)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<CreatorPayment> CreatePayment(CreateCreatorPaymentRequest request, CancellationToken cancellationToken = default)
        {
            CampaignCreator? campaignCreator = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .FirstOrDefaultAsync(item => item.Id == request.CampaignCreatorId, cancellationToken);

            if (campaignCreator is null || campaignCreator.Creator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            CreatorPayment payment = new(
                campaignCreator.Id,
                campaignCreator.CreatorId,
                request.GrossAmount,
                request.Discounts,
                request.Method,
                request.Description,
                request.CampaignDocumentId);

            if (!string.IsNullOrWhiteSpace(campaignCreator.Creator.PixKey) && campaignCreator.Creator.PixKeyType.HasValue)
            {
                payment.SnapshotPixDestination(campaignCreator.Creator.PixKey, campaignCreator.Creator.PixKeyType.Value);
            }

            payment.RegisterEvent(CreatorPaymentEventType.Created, $"Pagamento criado para {campaignCreator.Creator.Name}.");

            bool success = await Insert(cancellationToken, payment);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(payment.Id, cancellationToken) ?? payment;
        }

        public async Task<CreatorPayment> UpdatePayment(long id, UpdateCreatorPaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            payment.Update(request.GrossAmount, request.Discounts, request.Method, request.Description);
            payment.RegisterEvent(CreatorPaymentEventType.Updated);

            CreatorPayment? result = await Update(payment, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CreatorPayment> AttachInvoice(long id, AttachInvoiceRequest request, CancellationToken cancellationToken = default)
        {
            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            payment.AttachInvoice(request.InvoiceNumber, request.InvoiceUrl, request.IssuedAt);
            payment.RegisterEvent(CreatorPaymentEventType.InvoiceAttached, $"NF #{request.InvoiceNumber ?? "(sem numero)"} anexada.");

            CreatorPayment? result = await Update(payment, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CreatorPayment> MarkPaid(long id, MarkCreatorPaymentPaidRequest request, CancellationToken cancellationToken = default)
        {
            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (!string.IsNullOrWhiteSpace(request.Provider) && !string.IsNullOrWhiteSpace(request.ProviderTransactionId))
            {
                payment.AttachToProvider(request.Provider, request.ProviderTransactionId);
            }

            payment.MarkPaid(request.PaidAt);
            payment.RegisterEvent(CreatorPaymentEventType.Paid, "Marcado manualmente como pago.", null, request.PaidAt);

            CreatorPayment? result = await Update(payment, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CreatorPayment> Cancel(long id, CancellationToken cancellationToken = default)
        {
            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            payment.Cancel();
            payment.RegisterEvent(CreatorPaymentEventType.Cancelled);

            CreatorPayment? result = await Update(payment, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(result.Id, cancellationToken) ?? result;
        }

        public async Task<List<CreatorPayment>> SchedulePaymentBatch(SchedulePaymentBatchRequest request, CancellationToken cancellationToken = default)
        {
            DateTimeOffset scheduledFor = request.ScheduledFor ?? DateTimeOffset.UtcNow;

            List<CreatorPayment> payments = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Creator)
                .Include(item => item.CampaignCreator)
                .Include(item => item.Events)
                .Where(item => request.CreatorPaymentIds.Contains(item.Id))
                .ToListAsync(cancellationToken);

            if (payments.Count == 0)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            List<CreatorPayment> processed = [];

            foreach (CreatorPayment payment in payments)
            {
                if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Failed)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(payment.Creator?.PixKey))
                {
                    payment.RegisterEvent(CreatorPaymentEventType.ProviderSyncError, "Creator sem chave PIX cadastrada.");
                    continue;
                }

                payment.Schedule(scheduledFor);
                payment.RegisterEvent(CreatorPaymentEventType.Scheduled, $"Agendado para {scheduledFor:yyyy-MM-dd HH:mm}.");

                string payload = JsonSerializer.Serialize(new
                {
                    creatorPaymentId = payment.Id,
                    grossAmount = payment.GrossAmount,
                    discounts = payment.Discounts,
                    netAmount = payment.NetAmount,
                    description = payment.Description,
                    method = payment.Method.ToString(),
                    pixKey = payment.PixKey ?? payment.Creator?.PixKey,
                    pixKeyType = (payment.PixKeyType ?? payment.Creator?.PixKeyType)?.ToString(),
                    creatorName = payment.Creator?.Name,
                    creatorDocument = payment.Creator?.Document,
                    scheduledFor,
                });

                EnqueuePipelineRequest enqueueRequest = new()
                {
                    ConnectorId = request.ConnectorId,
                    PipelineId = request.PipelineId,
                    Payload = payload,
                    Priority = 1,
                };

                try
                {
                    await integrationPlatformClient.EnqueuePipelineAsync(enqueueRequest, cancellationToken);
                    payment.RegisterEvent(CreatorPaymentEventType.ProviderAccepted, $"Enfileirado no IntegrationPlatform via pipeline {request.PipelineId}.");
                    processed.Add(payment);
                }
                catch (Exception ex)
                {
                    payment.MarkFailed(ex.Message);
                    payment.RegisterEvent(CreatorPaymentEventType.ProviderSyncError, ex.Message);
                }
            }

            await DbContext.SaveChangesAsync(cancellationToken);

            return processed;
        }

        public async Task<CreatorPayment> HandleProviderCallback(CreatorPaymentProviderCallbackRequest request, CancellationToken cancellationToken = default)
        {
            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Provider == request.Provider && item.ProviderTransactionId == request.ProviderTransactionId, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            DateTimeOffset occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
            string normalizedEvent = request.EventType.Trim().ToLowerInvariant();

            switch (normalizedEvent)
            {
                case "paid":
                case "completed":
                case "transfer.completed":
                    payment.MarkPaid(occurredAt);
                    payment.RegisterEvent(CreatorPaymentEventType.Paid, request.EventType, request.Metadata, occurredAt);
                    break;

                case "failed":
                case "transfer.failed":
                    payment.MarkFailed(request.FailureReason, occurredAt);
                    payment.RegisterEvent(CreatorPaymentEventType.Failed, request.EventType, request.Metadata, occurredAt);
                    break;

                case "cancelled":
                case "transfer.cancelled":
                    payment.Cancel(request.FailureReason);
                    payment.RegisterEvent(CreatorPaymentEventType.Cancelled, request.EventType, request.Metadata, occurredAt);
                    break;

                default:
                    payment.RegisterEvent(CreatorPaymentEventType.ProviderSyncError, $"Evento desconhecido: {request.EventType}", request.Metadata, occurredAt);
                    break;
            }

            CreatorPayment? result = await Update(payment, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetPaymentById(result.Id, cancellationToken) ?? result;
        }

        private IQueryable<CreatorPayment> QueryWithDetails()
        {
            return DbContext.Set<CreatorPayment>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Campaign)
                .Include(item => item.CampaignDocument)
                .Include(item => item.Events.OrderByDescending(evt => evt.OccurredAt));
        }
    }
}
