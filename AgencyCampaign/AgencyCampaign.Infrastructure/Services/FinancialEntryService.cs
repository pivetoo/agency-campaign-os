using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Requests.FinancialEntries;
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
    public sealed class FinancialEntryService : CrudService<FinancialEntry>, IFinancialEntryService
    {
        private readonly IAutomationDispatcher automationDispatcher;
        private readonly INotificationService notificationService;
        private readonly ILogger<FinancialEntryService> logger;

        public FinancialEntryService(DbContext dbContext, IAutomationDispatcher automationDispatcher, INotificationService notificationService, ILogger<FinancialEntryService> logger) : base(dbContext)
        {
            this.automationDispatcher = automationDispatcher;
            this.notificationService = notificationService;
            this.logger = logger;
        }

        public async Task<PagedResult<FinancialEntry>> GetEntries(PagedRequest request, FinancialEntryFilters filters, CancellationToken cancellationToken = default)
        {
            await RecalculateOverdueAsync(cancellationToken);

            IQueryable<FinancialEntry> query = QueryWithDetails();
            query = ApplyFilters(query, filters);

            return await query
                .OrderByDescending(item => item.DueAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<FinancialEntry?> GetEntryById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<FinancialEntry>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<FinancialEntry> CreateEntry(CreateFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.AccountId, request.CampaignId, request.CampaignDeliverableId, cancellationToken);
            await EnsureSubcategoryExists(request.SubcategoryId, cancellationToken);
            await EnsurePeriodOpenAsync(request.OccurredAt, cancellationToken);

            FinancialEntry entry = BuildEntry(request);

            entry.ChangeStatus(request.Status, request.PaidAt);
            entry.RecalculateOverdue(DateTimeOffset.UtcNow);

            bool success = await Insert(cancellationToken, entry);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            string createdTrigger = entry.Type == FinancialEntryType.Receivable
                ? AutomationTriggers.FinancialReceivableCreated
                : AutomationTriggers.FinancialPayableCreated;
            await DispatchAutomationAsync(entry, createdTrigger, cancellationToken);

            if (entry.Status == FinancialEntryStatus.Paid)
            {
                string settledTrigger = entry.Type == FinancialEntryType.Receivable
                    ? AutomationTriggers.FinancialReceivableSettled
                    : AutomationTriggers.FinancialPayableSettled;
                await DispatchAutomationAsync(entry, settledTrigger, cancellationToken);
            }

            return await GetEntryById(entry.Id, cancellationToken) ?? entry;
        }

        public async Task<IReadOnlyCollection<FinancialEntry>> CreateInstallmentSeries(CreateInstallmentSeriesRequest request, CancellationToken cancellationToken = default)
        {
            if (request.InstallmentTotal < 2)
            {
                throw new InvalidOperationException("financialEntry.installments.totalRequired");
            }

            await EnsureReferencesExist(request.AccountId, request.CampaignId, request.CampaignDeliverableId, cancellationToken);
            await EnsureSubcategoryExists(request.SubcategoryId, cancellationToken);
            await EnsurePeriodOpenAsync(request.OccurredAt, cancellationToken);

            decimal baseAmount = Math.Round(request.Amount / request.InstallmentTotal, 2, MidpointRounding.AwayFromZero);
            decimal lastAmount = request.Amount - (baseAmount * (request.InstallmentTotal - 1));

            DateTimeOffset firstDueAt = request.DueAt;
            List<FinancialEntry> entries = [];

            for (int index = 1; index <= request.InstallmentTotal; index++)
            {
                decimal installmentAmount = index == request.InstallmentTotal ? lastAmount : baseAmount;
                DateTimeOffset installmentDueAt = firstDueAt.AddMonths(index - 1);

                CreateFinancialEntryRequest installmentRequest = new()
                {
                    AccountId = request.AccountId,
                    CampaignId = request.CampaignId,
                    CampaignDeliverableId = request.CampaignDeliverableId,
                    Type = request.Type,
                    Category = request.Category,
                    Description = $"{request.Description} ({index}/{request.InstallmentTotal})",
                    Amount = installmentAmount,
                    DueAt = installmentDueAt,
                    OccurredAt = request.OccurredAt,
                    PaymentMethod = request.PaymentMethod,
                    ReferenceCode = request.ReferenceCode,
                    PaidAt = null,
                    Status = FinancialEntryStatus.Pending,
                    CounterpartyName = request.CounterpartyName,
                    Notes = request.Notes,
                    SubcategoryId = request.SubcategoryId,
                    InvoiceNumber = request.InvoiceNumber,
                    InvoiceUrl = request.InvoiceUrl,
                    InvoiceIssuedAt = request.InvoiceIssuedAt
                };

                FinancialEntry entry = BuildEntry(installmentRequest);
                entry.MarkAsInstallment(null, index, request.InstallmentTotal);
                entry.RecalculateOverdue(DateTimeOffset.UtcNow);

                DbContext.Set<FinancialEntry>().Add(entry);
                entries.Add(entry);
            }

            await DbContext.SaveChangesAsync(cancellationToken);

            long parentId = entries[0].Id;
            for (int index = 1; index < entries.Count; index++)
            {
                entries[index].MarkAsInstallment(parentId, index + 1, request.InstallmentTotal);
            }
            await DbContext.SaveChangesAsync(cancellationToken);

            string createdTrigger = request.Type == FinancialEntryType.Receivable
                ? AutomationTriggers.FinancialReceivableCreated
                : AutomationTriggers.FinancialPayableCreated;

            foreach (FinancialEntry entry in entries)
            {
                await DispatchAutomationAsync(entry, createdTrigger, cancellationToken);
            }

            return entries;
        }

        private FinancialEntry BuildEntry(CreateFinancialEntryRequest request)
        {
            FinancialEntry entry = new(
                request.AccountId,
                request.Type,
                request.Category,
                request.Description,
                request.Amount,
                request.DueAt,
                request.OccurredAt,
                request.PaymentMethod,
                request.ReferenceCode,
                request.CounterpartyName,
                request.Notes,
                request.CampaignId,
                request.CampaignDeliverableId);

            entry.SetSubcategory(request.SubcategoryId);
            entry.SetInvoice(request.InvoiceNumber, request.InvoiceUrl, request.InvoiceIssuedAt);
            return entry;
        }

        private async Task EnsureSubcategoryExists(long? subcategoryId, CancellationToken cancellationToken)
        {
            if (!subcategoryId.HasValue)
            {
                return;
            }

            bool exists = await DbContext.Set<FinancialSubcategory>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == subcategoryId.Value && item.IsActive, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        public async Task<FinancialEntry> UpdateEntry(long id, UpdateFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            FinancialEntry? entry = await DbContext.Set<FinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsureReferencesExist(request.AccountId, request.CampaignId, request.CampaignDeliverableId, cancellationToken);
            await EnsureSubcategoryExists(request.SubcategoryId, cancellationToken);
            await EnsurePeriodOpenAsync(entry.OccurredAt, cancellationToken);
            await EnsurePeriodOpenAsync(request.OccurredAt, cancellationToken);

            entry.Update(
                request.AccountId,
                request.Type,
                request.Category,
                request.Description,
                request.Amount,
                request.DueAt,
                request.OccurredAt,
                request.PaymentMethod,
                request.ReferenceCode,
                request.CounterpartyName,
                request.Notes,
                request.CampaignId,
                request.CampaignDeliverableId);

            entry.SetSubcategory(request.SubcategoryId);
            entry.SetInvoice(request.InvoiceNumber, request.InvoiceUrl, request.InvoiceIssuedAt);

            entry.ChangeStatus(request.Status, request.PaidAt);
            entry.RecalculateOverdue(DateTimeOffset.UtcNow);

            FinancialEntry? result = await Update(entry, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetEntryById(result.Id, cancellationToken) ?? result;
        }

        public async Task<FinancialEntry> MarkAsPaid(long id, MarkAsPaidRequest request, CancellationToken cancellationToken = default)
        {
            FinancialEntry? entry = await DbContext.Set<FinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool accountExists = await DbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.AccountId && item.IsActive, cancellationToken);

            if (!accountExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsurePeriodOpenAsync(request.PaidAt ?? DateTimeOffset.UtcNow, cancellationToken);

            entry.Update(
                request.AccountId,
                entry.Type,
                entry.Category,
                entry.Description,
                entry.Amount,
                entry.DueAt,
                entry.OccurredAt,
                request.PaymentMethod ?? entry.PaymentMethod,
                entry.ReferenceCode,
                entry.CounterpartyName,
                entry.Notes,
                entry.CampaignId,
                entry.CampaignDeliverableId);

            entry.ChangeStatus(FinancialEntryStatus.Paid, request.PaidAt ?? DateTimeOffset.UtcNow);

            FinancialEntry? result = await Update(entry, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            string settledTrigger = entry.Type == FinancialEntryType.Receivable
                ? AutomationTriggers.FinancialReceivableSettled
                : AutomationTriggers.FinancialPayableSettled;
            await DispatchAutomationAsync(entry, settledTrigger, cancellationToken);

            try
            {
                await notificationService.Create(KanvasNotifications.FinancialEntrySettled(entry), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to create notification for settled financial entry {Id}.", entry.Id);
            }

            return await GetEntryById(result.Id, cancellationToken) ?? result;
        }

        // Estorno (D3b): cria a contrapartida do lancamento pago numa unica transacao (marca o original
        // estornado + insere a contrapartida ja paga). Quando o estornado e um repasse (CreatorPayout) e ja
        // existe um CreatorPayment PAGO via PIX para o mesmo creator/campanha, sinaliza - o estorno contabil
        // NAO desfaz o pagamento real ao creator.
        public async Task<ReverseEntryResult> ReverseEntry(long id, ReverseFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            FinancialEntry? original = await DbContext.Set<FinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (original is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            DateTimeOffset reversedAt = DateTimeOffset.UtcNow;
            string reason = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $": {request.Reason.Trim()}";
            string description = $"Estorno do lancamento #{original.Id}{reason}";

            original.MarkAsReversed(reversedAt);
            FinancialEntry reversal = original.BuildReversalEntry(reversedAt, description);

            bool creatorPaymentAlreadyPaid = false;
            if (original.Category == FinancialEntryCategory.CreatorPayout && original.CampaignId.HasValue && original.CreatorId.HasValue)
            {
                creatorPaymentAlreadyPaid = await DbContext.Set<CreatorPayment>()
                    .AsNoTracking()
                    .AnyAsync(item => item.CreatorId == original.CreatorId
                        && item.Status == PaymentStatus.Paid
                        && item.CampaignCreator!.CampaignId == original.CampaignId, cancellationToken);

                if (creatorPaymentAlreadyPaid)
                {
                    reversal.AppendNote("ATENCAO: ja existe repasse pago via PIX para este creator/campanha; o estorno contabil nao desfaz o pagamento real.");
                }
            }

            DbContext.Set<FinancialEntry>().Add(reversal);
            await DbContext.SaveChangesAsync(cancellationToken);

            FinancialEntry persisted = await GetEntryById(reversal.Id, cancellationToken) ?? reversal;
            return new ReverseEntryResult(persisted, creatorPaymentAlreadyPaid);
        }

        public async Task<FinancialSummaryModel> GetSummary(FinancialEntryType type, CancellationToken cancellationToken = default)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset firstDayOfMonth = new(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset next7Days = now.AddDays(7);

            List<FinancialEntry> entries = await DbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.Type == type)
                .ToListAsync(cancellationToken);

            // Vencido calculado por data (DueAt < now) sobre os lancamentos ABERTOS (Pendente|Vencido), sem
            // depender do status persistido e sem ESCREVER durante o GET (D6): o resumo fica exato mesmo que o
            // recalculo de vencidos ainda nao tenha rodado. A lista (GetEntries) segue mantendo o status
            // persistido para os filtros e automacoes que leem o status.
            List<FinancialEntry> open = entries
                .Where(item => item.Status == FinancialEntryStatus.Pending || item.Status == FinancialEntryStatus.Overdue)
                .ToList();

            return new FinancialSummaryModel
            {
                Type = type,
                TotalPending = open.Where(item => item.DueAt >= now).Sum(item => item.Amount),
                // Exclui o par estornado (original IsReversed + contrapartida ReversalOfEntryId), igual ao relatorio,
                // para o KPI "recebido/pago no mes" da tela bater com o relatorio e nao contar estorno em dobro.
                TotalSettledThisMonth = entries.Where(item => item.Status == FinancialEntryStatus.Paid && item.PaidAt.HasValue && item.PaidAt.Value >= firstDayOfMonth && !item.IsReversed && item.ReversalOfEntryId == null).Sum(item => item.Amount),
                TotalOverdue = open.Where(item => item.DueAt < now).Sum(item => item.Amount),
                TotalDueNext7Days = open.Where(item => item.DueAt >= now && item.DueAt <= next7Days).Sum(item => item.Amount),
                PendingCount = open.Count(item => item.DueAt >= now),
                OverdueCount = open.Count(item => item.DueAt < now)
            };
        }

        private async Task RecalculateOverdueAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            await DbContext.Set<FinancialEntry>()
                .Where(item => item.Status == FinancialEntryStatus.Pending && item.DueAt < now)
                .ExecuteUpdateAsync(setter => setter.SetProperty(item => item.Status, FinancialEntryStatus.Overdue), cancellationToken);

            await DbContext.Set<FinancialEntry>()
                .Where(item => item.Status == FinancialEntryStatus.Overdue && item.DueAt >= now)
                .ExecuteUpdateAsync(setter => setter.SetProperty(item => item.Status, FinancialEntryStatus.Pending), cancellationToken);
        }

        private static IQueryable<FinancialEntry> ApplyFilters(IQueryable<FinancialEntry> query, FinancialEntryFilters filters)
        {
            if (filters.Type.HasValue)
            {
                query = query.Where(item => item.Type == filters.Type.Value);
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(item => item.Status == filters.Status.Value);
            }

            if (filters.AccountId.HasValue)
            {
                query = query.Where(item => item.AccountId == filters.AccountId.Value);
            }

            if (filters.CampaignId.HasValue)
            {
                query = query.Where(item => item.CampaignId == filters.CampaignId.Value);
            }

            if (filters.DueFrom.HasValue)
            {
                query = query.Where(item => item.DueAt >= filters.DueFrom.Value);
            }

            if (filters.DueTo.HasValue)
            {
                query = query.Where(item => item.DueAt <= filters.DueTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                string term = filters.Search.Trim().ToLower();
                query = query.Where(item =>
                    item.Description.ToLower().Contains(term) ||
                    (item.CounterpartyName != null && item.CounterpartyName.ToLower().Contains(term)) ||
                    (item.ReferenceCode != null && item.ReferenceCode.ToLower().Contains(term)));
            }

            return query;
        }

        // Fechamento de periodo (D3c): bloqueia escrita de dinheiro datada num mes ja fechado (back-dating).
        // O estorno NAO passa por aqui - ele lanca a contrapartida no mes aberto corrente (correcao contabil).
        private async Task EnsurePeriodOpenAsync(DateTimeOffset date, CancellationToken cancellationToken)
        {
            DateTimeOffset utc = date.ToUniversalTime();
            bool closed = await DbContext.Set<FinancialPeriod>()
                .AsNoTracking()
                .AnyAsync(item => item.Year == utc.Year && item.Month == utc.Month && item.IsClosed, cancellationToken);

            if (closed)
            {
                throw new InvalidOperationException("financialPeriod.closed");
            }
        }

        private async Task EnsureReferencesExist(long accountId, long? campaignId, long? campaignDeliverableId, CancellationToken cancellationToken)
        {
            bool accountExists = await DbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == accountId && item.IsActive, cancellationToken);

            if (!accountExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            if (campaignId.HasValue)
            {
                bool campaignExists = await DbContext.Set<Campaign>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == campaignId.Value, cancellationToken);

                if (!campaignExists)
                {
                    throw new InvalidOperationException("record.notFound");
                }
            }

            if (campaignDeliverableId.HasValue)
            {
                bool deliverableExists = await DbContext.Set<CampaignDeliverable>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == campaignDeliverableId.Value, cancellationToken);

                if (!deliverableExists)
                {
                    throw new InvalidOperationException("record.notFound");
                }
            }
        }

        private IQueryable<FinancialEntry> QueryWithDetails()
        {
            return DbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Include(item => item.Account)
                .Include(item => item.Campaign)
                .Include(item => item.CampaignDeliverable)
                .Include(item => item.Subcategory);
        }

        private async Task DispatchAutomationAsync(FinancialEntry entry, string trigger, CancellationToken cancellationToken)
        {
            FinancialEntry? full = await GetEntryById(entry.Id, cancellationToken);
            FinancialEntry source = full ?? entry;

            Dictionary<string, object?> payload = new(StringComparer.OrdinalIgnoreCase)
            {
                ["entryId"] = source.Id,
                ["type"] = (int)source.Type,
                ["category"] = (int)source.Category,
                ["description"] = source.Description,
                ["amount"] = source.Amount,
                ["dueAt"] = source.DueAt.ToString("yyyy-MM-dd"),
                ["dueAtBR"] = source.DueAt.ToString("dd/MM/yyyy"),
                ["paidAt"] = source.PaidAt?.ToString("yyyy-MM-dd"),
                ["paidAtBR"] = source.PaidAt?.ToString("dd/MM/yyyy"),
                ["status"] = (int)source.Status,
                ["counterpartyName"] = source.CounterpartyName,
                ["paymentMethod"] = source.PaymentMethod,
                ["referenceCode"] = source.ReferenceCode,
                ["accountId"] = source.AccountId,
                ["accountName"] = source.Account?.Name,
                ["campaignId"] = source.CampaignId,
                ["campaignName"] = source.Campaign?.Name,
                ["invoiceNumber"] = source.InvoiceNumber,
                ["invoiceUrl"] = source.InvoiceUrl,
                ["invoiceIssuedAt"] = source.InvoiceIssuedAt?.ToString("yyyy-MM-dd"),
                ["installmentNumber"] = source.InstallmentNumber,
                ["installmentTotal"] = source.InstallmentTotal
            };

            try
            {
                await automationDispatcher.DispatchAsync(trigger, payload, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to dispatch automation '{Trigger}' for financial entry {Id}.", trigger, entry.Id);
            }
        }
    }
}
