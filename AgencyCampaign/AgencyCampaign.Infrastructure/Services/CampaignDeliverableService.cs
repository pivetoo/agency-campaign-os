using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.CampaignDeliverables;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDeliverableService : CrudService<CampaignDeliverable>, ICampaignDeliverableService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly IFinancialAutoGeneration financialAutoGeneration;
        private readonly INotificationService notificationService;
        private readonly ILogger<CampaignDeliverableService> logger;
        private readonly IContentFileStorage? fileStorage;

        public CampaignDeliverableService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, IFinancialAutoGeneration financialAutoGeneration, INotificationService notificationService, ILogger<CampaignDeliverableService> logger, IContentFileStorage? fileStorage = null) : base(dbContext)
        {
            this.localizer = localizer;
            this.financialAutoGeneration = financialAutoGeneration;
            this.notificationService = notificationService;
            this.logger = logger;
            this.fileStorage = fileStorage;
        }


        public async Task<PagedResult<CampaignDeliverable>> GetDeliverables(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.DueAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignDeliverable?> GetDeliverableById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignDeliverable>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderBy(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CampaignDeliverable>> GetForCalendar(CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.Campaign!.IsActive)
                .OrderBy(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignDeliverable> CreateDeliverable(CreateCampaignDeliverableRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CampaignCreatorId, request.DeliverableKindId, request.PlatformId, cancellationToken);

            CampaignDeliverable deliverable = new(
                request.CampaignId,
                request.CampaignCreatorId,
                request.Title,
                request.DeliverableKindId,
                request.PlatformId,
                request.DueAt,
                request.GrossAmount,
                request.CreatorAmount,
                request.AgencyFeeAmount,
                request.Description,
                request.Notes);

            await ApplyPublishing(deliverable, request.Status, request.PublishedUrl, request.EvidenceUrl, cancellationToken);

            bool success = await Insert(cancellationToken, deliverable);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            if (deliverable.Status == DeliverableStatus.Published)
            {
                await TryGenerateCreatorPayout(deliverable, cancellationToken);
            }

            await AdvanceCampaignToInProgressAsync(request.CampaignId, cancellationToken);

            return await GetDeliverableById(deliverable.Id, cancellationToken) ?? deliverable;
        }

        // M1 (status automatico): criar um entregavel inicia a execucao da campanha (Draft/Planejada -> Executando).
        private async Task AdvanceCampaignToInProgressAsync(long campaignId, CancellationToken cancellationToken)
        {
            Campaign? campaign = await DbContext.Set<Campaign>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);

            if (campaign is not null && (campaign.Status == CampaignStatus.Draft || campaign.Status == CampaignStatus.Planned))
            {
                campaign.ChangeStatus(CampaignStatus.InProgress);
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<CampaignDeliverable> UpdateDeliverable(long id, UpdateCampaignDeliverableRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            CampaignDeliverable? deliverable = await DbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .Include(item => item.Approvals)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (deliverable is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsureReferencesExist(deliverable.CampaignId, deliverable.CampaignCreatorId, request.DeliverableKindId, request.PlatformId, cancellationToken);

            DeliverableStatus previousStatus = deliverable.Status;

            deliverable.Update(
                request.Title,
                request.DeliverableKindId,
                request.PlatformId,
                request.DueAt,
                request.GrossAmount,
                request.CreatorAmount,
                request.AgencyFeeAmount,
                request.Description,
                request.Notes);

            await ApplyPublishing(deliverable, request.Status, request.PublishedUrl, request.EvidenceUrl, cancellationToken);
            ApplyMetrics(deliverable, request);

            CampaignDeliverable? result = await Update(deliverable, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            if (deliverable.Status == DeliverableStatus.Published && previousStatus != DeliverableStatus.Published)
            {
                await TryGenerateCreatorPayout(deliverable, cancellationToken);
            }

            return await GetDeliverableById(result.Id, cancellationToken) ?? result;
        }

        private static void ApplyMetrics(CampaignDeliverable deliverable, UpdateCampaignDeliverableRequest request)
        {
            bool hasAny = request.Likes.HasValue
                || request.Comments.HasValue
                || request.Views.HasValue
                || request.Reach.HasValue
                || request.Impressions.HasValue
                || request.Saves.HasValue
                || request.Shares.HasValue;

            if (!hasAny)
            {
                return;
            }

            // Merge: so sobrescreve a metrica que veio no request; campos ausentes (null) preservam o
            // valor atual, evitando que uma edicao manual de um campo zere os insights enviados pelo
            // creator ou coletados pelo Apify.
            deliverable.RegisterMetrics(
                request.Likes ?? deliverable.Likes,
                request.Comments ?? deliverable.Comments,
                request.Views ?? deliverable.Views,
                request.Reach ?? deliverable.Reach,
                request.Impressions ?? deliverable.Impressions,
                request.Saves ?? deliverable.Saves,
                request.Shares ?? deliverable.Shares,
                DeliverableMetricsSource.Manual);
        }

        // Lembrete proativo de prazo: notifica (agency-wide) entregaveis nao publicados/cancelados com
        // prazo vencido ou a vencer dentro da janela, deduplicando por DeadlineReminderSentAt (resetado
        // quando o prazo e remarcado). Espelha o lembrete de follow-up do comercial.
        public async Task<int> RemindDueDeliverables(int daysAhead, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset cutoff = now.AddDays(daysAhead <= 0 ? 3 : daysAhead);

            List<CampaignDeliverable> due = await DbContext.Set<CampaignDeliverable>()
                .AsTracking()
                .Where(item => item.Status != DeliverableStatus.Published
                    && item.Status != DeliverableStatus.Cancelled
                    && item.DeadlineReminderSentAt == null
                    && item.DueAt <= cutoff)
                .ToListAsync(cancellationToken);

            int reminded = 0;
            foreach (CampaignDeliverable deliverable in due)
            {
                try
                {
                    int daysUntilDue = (int)Math.Ceiling((deliverable.DueAt - now).TotalDays);
                    await notificationService.Create(KanvasNotifications.DeliverableDueSoon(deliverable, daysUntilDue), cancellationToken);
                    deliverable.MarkDeadlineReminderSent();
                    reminded++;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to remind deadline for deliverable {Id}.", deliverable.Id);
                }
            }

            if (reminded > 0)
            {
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            return reminded;
        }

        private async Task TryGenerateCreatorPayout(CampaignDeliverable deliverable, CancellationToken cancellationToken)
        {
            try
            {
                await financialAutoGeneration.GenerateForPublishedDeliverable(deliverable, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate creator payout for deliverable {Id}.", deliverable.Id);
                try
                {
                    await notificationService.Create(KanvasNotifications.PayoutGenerationFailed(deliverable), cancellationToken);
                }
                catch (Exception notificationException)
                {
                    logger.LogError(notificationException, "Failed to notify about payout generation failure for deliverable {Id}.", deliverable.Id);
                }
            }
        }

        public override async Task<CampaignDeliverable?> Delete(long id, CancellationToken cancellationToken = default)
        {
            MutableMessages.Clear();

            CampaignDeliverable? deliverable = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (deliverable is null)
            {
                MutableMessages.Add(new KeyNotFoundException(localizer["record.notFound"]));
                return null;
            }

            // M2 (GC de midia orfa): coleta as chaves de armazenamento privado antes da exclusao para limpar
            // os arquivos no disco depois (as linhas de versao/asset somem por cascade, mas os arquivos nao).
            List<string> assetKeys = (await DbContext.Set<DeliverableContentVersion>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == id)
                .SelectMany(item => item.Assets.Select(asset => asset.Url))
                .ToListAsync(cancellationToken))
                .Where(url => url.StartsWith("content/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool removed = await Delete([deliverable], cancellationToken);

            if (removed && fileStorage is not null && assetKeys.Count > 0)
            {
                fileStorage.RemoveByVersion(id, assetKeys);
            }

            return removed ? deliverable : null;
        }

        private async Task EnsureReferencesExist(long campaignId, long campaignCreatorId, long deliverableKindId, long platformId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool campaignCreatorExists = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignCreatorId && item.CampaignId == campaignId, cancellationToken);

            if (!campaignCreatorExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool deliverableKindExists = await DbContext.Set<DeliverableKind>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == deliverableKindId && item.IsActive, cancellationToken);

            if (!deliverableKindExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool platformExists = await DbContext.Set<Platform>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == platformId && item.IsActive, cancellationToken);

            if (!platformExists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        private async Task ApplyPublishing(CampaignDeliverable deliverable, DeliverableStatus status, string? publishedUrl, string? evidenceUrl, CancellationToken cancellationToken)
        {
            if (status == DeliverableStatus.Published)
            {
                if (string.IsNullOrWhiteSpace(publishedUrl))
                {
                    throw new InvalidOperationException("deliverable.publishedUrl.required");
                }

                await EnsureBrandApprovalAllowedAsync(deliverable, cancellationToken);

                deliverable.Publish(publishedUrl, evidenceUrl, DateTimeOffset.UtcNow);
                return;
            }

            deliverable.ChangeStatus(status);
            deliverable.UpdateEvidence(evidenceUrl);
        }

        // Gate de aprovacao para publicar: exige um DeliverableApproval Aprovado - da MARCA (link
        // publico) OU INTERNO (aprovacao da agencia, C8) -, A NAO SER que a campanha tenha o gate
        // desligado (RequiresDeliverableApproval = false). Default obrigatorio. Modelo unico: o gate
        // e a aprovacao interna falam o mesmo DeliverableApproval (D8i).
        private async Task EnsureBrandApprovalAllowedAsync(CampaignDeliverable deliverable, CancellationToken cancellationToken)
        {
            bool requiresApproval = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .Where(item => item.Id == deliverable.CampaignId)
                .Select(item => (bool?)item.RequiresDeliverableApproval)
                .FirstOrDefaultAsync(cancellationToken) ?? true;

            if (!requiresApproval)
            {
                return;
            }

            bool hasApproval = deliverable.Approvals.Any(item =>
                (item.ApprovalType == DeliverableApprovalType.Brand || item.ApprovalType == DeliverableApprovalType.Internal) &&
                item.Status == DeliverableApprovalStatus.Approved);

            if (!hasApproval)
            {
                throw new InvalidOperationException("deliverable.publish.brandApprovalRequired");
            }
        }

        private IQueryable<CampaignDeliverable> QueryWithDetails()
        {
            return DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.CampaignCreator!)
                    .ThenInclude(item => item.Creator)
                .Include(item => item.DeliverableKind)
                .Include(item => item.Platform)
                .Include(item => item.Approvals);
        }
    }
}
