using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Application.Abstractions;
using Archon.Application.MultiTenancy;
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
        private readonly IntegrationPlatformClient integrationPlatformClient;
        private readonly ITenantContext? tenantContext;
        private readonly ICurrentUser? currentUser;

        public CreatorPaymentService(DbContext dbContext, IntegrationPlatformClient integrationPlatformClient, ITenantContext? tenantContext = null, ICurrentUser? currentUser = null) : base(dbContext)
        {
            this.integrationPlatformClient = integrationPlatformClient;
            this.tenantContext = tenantContext;
            this.currentUser = currentUser;
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
                throw new InvalidOperationException("record.notFound");
            }

            CreatorPayment payment = new(
                campaignCreator.Id,
                campaignCreator.CreatorId,
                request.GrossAmount,
                request.Discounts,
                request.Method,
                request.Description,
                request.CampaignDocumentId,
                request.TaxWithheld);

            if (!string.IsNullOrWhiteSpace(campaignCreator.Creator.PixKey) && campaignCreator.Creator.PixKeyType.HasValue)
            {
                payment.SnapshotPixDestination(campaignCreator.Creator.PixKey, campaignCreator.Creator.PixKeyType.Value);
            }

            payment.SetCreatedBy(currentUser?.UserId);

            bool success = await Insert(cancellationToken, payment);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            // Registrar evento somente apos o Insert: o construtor de CreatorPaymentEvent exige
            // creatorPaymentId > 0, que so e atribuido pelo SaveChanges do Insert.
            await RegisterCreatedEventAsync(payment.Id, $"Pagamento criado para {campaignCreator.Creator.Name}.", cancellationToken);

            return await GetPaymentById(payment.Id, cancellationToken) ?? payment;
        }

        private async Task RegisterCreatedEventAsync(long paymentId, string description, CancellationToken cancellationToken)
        {
            CreatorPayment? tracked = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == paymentId, cancellationToken);

            if (tracked is null)
            {
                return;
            }

            tracked.RegisterEvent(CreatorPaymentEventType.Created, description);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<CreatorPayment> UpdatePayment(long id, UpdateCreatorPaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            payment.Update(request.GrossAmount, request.Discounts, request.Method, request.Description, request.TaxWithheld);
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
                throw new InvalidOperationException("record.notFound");
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
                throw new InvalidOperationException("record.notFound");
            }

            if (!string.IsNullOrWhiteSpace(request.Provider) && !string.IsNullOrWhiteSpace(request.ProviderTransactionId))
            {
                payment.AttachToProvider(request.Provider, request.ProviderTransactionId);
            }

            payment.MarkPaid(request.PaidAt);
            payment.RegisterEvent(CreatorPaymentEventType.Paid, "Marcado manualmente como pago.", null, request.PaidAt);
            await SettlePlannedPayoutsAsync(payment, request.PaidAt, cancellationToken);

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
                throw new InvalidOperationException("record.notFound");
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

        public async Task<CreatorPayment> ApprovePayment(long id, CancellationToken cancellationToken = default)
        {
            long? approverId = currentUser?.UserId;
            if (!approverId.HasValue)
            {
                throw new InvalidOperationException("creatorPayment.approverUnknown");
            }

            CreatorPayment? payment = await DbContext.Set<CreatorPayment>()
                .AsTracking()
                .Include(item => item.Events)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            payment.Approve(approverId.Value);
            payment.RegisterEvent(CreatorPaymentEventType.Approved, $"Aprovado pelo usuario #{approverId.Value}.");

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
                throw new InvalidOperationException("record.notFound");
            }

            // Teto de alcada (maker-checker): acima deste valor liquido, o repasse so e agendado apos aprovacao
            // por usuario diferente de quem registrou. Nulo = sem teto. Lido uma vez por lote.
            decimal? approvalThreshold = await DbContext.Set<AgencySettings>()
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .Select(item => item.CreatorPaymentApprovalThreshold)
                .FirstOrDefaultAsync(cancellationToken);

            // Pay-when-paid (E2): para campanhas com o gate ligado, so agenda quando TODOS os entregaveis do
            // creator naquela campanha estiverem aprovados. Sem entregaveis = libera. Pre-carregado (evita N+1).
            List<long> campaignCreatorIds = payments.Select(item => item.CampaignCreatorId).Distinct().ToList();

            Dictionary<long, long> campaignByCampaignCreator = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => campaignCreatorIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.CampaignId, cancellationToken);

            HashSet<long> gatedCampaigns = (await DbContext.Set<Campaign>()
                .AsNoTracking()
                .Where(item => item.PayoutRequiresContentApproval)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken)).ToHashSet();

            var deliverableApprovals = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => campaignCreatorIds.Contains(item.CampaignCreatorId))
                .Select(item => new
                {
                    item.CampaignCreatorId,
                    Approved = item.Approvals.Any(approval => (approval.ApprovalType == DeliverableApprovalType.Brand || approval.ApprovalType == DeliverableApprovalType.Internal) && approval.Status == DeliverableApprovalStatus.Approved)
                })
                .ToListAsync(cancellationToken);

            ILookup<long, bool> approvalsByCampaignCreator = deliverableApprovals.ToLookup(item => item.CampaignCreatorId, item => item.Approved);

            List<CreatorPayment> processed = [];

            foreach (CreatorPayment payment in payments)
            {
                if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Failed)
                {
                    continue;
                }

                if (campaignByCampaignCreator.TryGetValue(payment.CampaignCreatorId, out long campaignId) && gatedCampaigns.Contains(campaignId))
                {
                    List<bool> deliverableStates = approvalsByCampaignCreator[payment.CampaignCreatorId].ToList();
                    bool contentBlocked = deliverableStates.Count > 0 && deliverableStates.Any(approved => !approved);
                    if (contentBlocked)
                    {
                        payment.RegisterEvent(CreatorPaymentEventType.ContentApprovalRequired, "Conteudo do entregavel ainda nao aprovado - repasse bloqueado ate a aprovacao (pay-when-paid).");
                        continue;
                    }
                }

                if (approvalThreshold.HasValue && payment.NetAmount > approvalThreshold.Value && !payment.IsApproved)
                {
                    payment.RegisterEvent(CreatorPaymentEventType.ApprovalRequired, $"Repasse liquido de R$ {payment.NetAmount:0.00} acima do teto de R$ {approvalThreshold.Value:0.00} - requer aprovacao antes do agendamento.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(payment.Creator?.PixKey))
                {
                    payment.RegisterEvent(CreatorPaymentEventType.ProviderSyncError, "Creator sem chave PIX cadastrada.");
                    continue;
                }

                payment.Schedule(scheduledFor);
                payment.AssignIdempotencyKey(Guid.NewGuid().ToString("N"));
                payment.RegisterEvent(CreatorPaymentEventType.Scheduled, $"Agendado para {scheduledFor:yyyy-MM-dd HH:mm}.");

                bool requiresInvoice = payment.Creator?.TaxRegime is TaxRegime regime && regime != TaxRegime.IndividualPF;
                bool hasInvoice = !string.IsNullOrWhiteSpace(payment.InvoiceNumber) || !string.IsNullOrWhiteSpace(payment.InvoiceUrl);
                if (requiresInvoice && !hasInvoice)
                {
                    payment.RegisterEvent(CreatorPaymentEventType.InvoiceMissing, "Creator PJ sem nota fiscal anexada - emitir/anexar a NFS-e para regularizar o repasse.");
                }

                string payload = JsonSerializer.Serialize(new
                {
                    creatorPaymentId = payment.Id,
                    idempotencyKey = payment.IdempotencyKey,
                    // Token tenant-scoped para o pipeline ECOAR na URL do callback (/api/creatorpayments/provider-callback/{callbackToken}),
                    // permitindo ao Kanvas resolver o tenant correto no multi-tenant. Fallback = segredo global (transicao).
                    callbackToken = PublicLinkToken.Compose(tenantContext?.TenantId, payment.IdempotencyKey ?? string.Empty),
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
                throw new InvalidOperationException("record.notFound");
            }

            DateTimeOffset occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
            string normalizedEvent = request.EventType.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(request.EndToEndId))
            {
                payment.AttachEndToEndId(request.EndToEndId);
            }

            // Idempotencia: o provedor reentrega o mesmo evento (entrega at-least-once). Se o pagamento ja
            // esta no estado final correspondente, nao re-dispara transicao/evento/baixa - so persiste o e2eId.
            bool alreadyHandled =
                ((normalizedEvent is "paid" or "completed" or "transfer.completed") && payment.Status == PaymentStatus.Paid) ||
                ((normalizedEvent is "failed" or "transfer.failed") && payment.Status == PaymentStatus.Failed) ||
                ((normalizedEvent is "cancelled" or "transfer.cancelled") && payment.Status == PaymentStatus.Cancelled);

            if (alreadyHandled)
            {
                CreatorPayment? unchanged = await Update(payment, cancellationToken);
                return await GetPaymentById((unchanged ?? payment).Id, cancellationToken) ?? payment;
            }

            switch (normalizedEvent)
            {
                case "paid":
                case "completed":
                case "transfer.completed":
                    payment.MarkPaid(occurredAt);
                    payment.RegisterEvent(CreatorPaymentEventType.Paid, request.EventType, request.Metadata, occurredAt);
                    await SettlePlannedPayoutsAsync(payment, occurredAt, cancellationToken);
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

        // Baixa automatica (conciliacao): ao marcar um CreatorPayment como pago, da baixa nos repasses
        // PREVISTOS (FinancialEntry CreatorPayout, Pending/Overdue) do mesmo campanha+creator. Avisa,
        // via evento, quando a soma do previsto diverge do valor liquido efetivamente pago.
        private async Task SettlePlannedPayoutsAsync(CreatorPayment payment, DateTimeOffset paidAt, CancellationToken cancellationToken)
        {
            long? campaignId = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => item.Id == payment.CampaignCreatorId)
                .Select(item => (long?)item.CampaignId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!campaignId.HasValue)
            {
                return;
            }

            List<FinancialEntry> planned = await DbContext.Set<FinancialEntry>()
                .AsTracking()
                .Where(item => item.CampaignId == campaignId
                    && item.CreatorId == payment.CreatorId
                    && item.Type == FinancialEntryType.Payable
                    && item.Category == FinancialEntryCategory.CreatorPayout
                    && (item.Status == FinancialEntryStatus.Pending || item.Status == FinancialEntryStatus.Overdue))
                .ToListAsync(cancellationToken);

            if (planned.Count == 0)
            {
                return;
            }

            // Vinculo 1:1 (D4): baixa os repasses previstos por ordem de vencimento, sem ultrapassar o valor
            // efetivamente pago. Evita quitar mais previsto do que o pagamento cobre; a sobra fica em aberto.
            List<FinancialEntry> ordered = planned.OrderBy(item => item.DueAt).ToList();
            decimal remaining = payment.NetAmount;
            List<FinancialEntry> settled = [];

            foreach (FinancialEntry entry in ordered)
            {
                if (entry.Amount > remaining)
                {
                    continue;
                }

                entry.ChangeStatus(FinancialEntryStatus.Paid, paidAt);
                remaining -= entry.Amount;
                settled.Add(entry);
            }

            decimal plannedTotal = planned.Sum(item => item.Amount);

            if (settled.Count == 0)
            {
                payment.RegisterEvent(CreatorPaymentEventType.PlannedPayoutSettled,
                    $"Pagamento de R$ {payment.NetAmount:0.00} diverge do previsto: nenhum repasse previsto coube no valor pago (menor previsto e maior que o pago); nada baixado.");
                return;
            }

            decimal settledTotal = settled.Sum(item => item.Amount);
            bool fullyReconciled = settledTotal == payment.NetAmount && settled.Count == planned.Count;
            string description = fullyReconciled
                ? $"Baixa automatica de {settled.Count} repasse(s) previsto(s) (R$ {settledTotal:0.00})."
                : $"Baixa automatica de {settled.Count} de {planned.Count} repasse(s) previsto(s) (R$ {settledTotal:0.00}); valor pago R$ {payment.NetAmount:0.00} diverge do previsto total R$ {plannedTotal:0.00}.";

            payment.RegisterEvent(CreatorPaymentEventType.PlannedPayoutSettled, description);
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
