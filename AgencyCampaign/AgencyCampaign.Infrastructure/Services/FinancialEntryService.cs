using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialEntryService : CrudService<FinancialEntry>, IFinancialEntryService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public FinancialEntryService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
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

            entry.ChangeStatus(request.Status, request.PaidAt);
            entry.RecalculateOverdue(DateTimeOffset.UtcNow);

            bool success = await Insert(cancellationToken, entry);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetEntryById(entry.Id, cancellationToken) ?? entry;
        }

        public async Task<FinancialEntry> UpdateEntry(long id, UpdateFinancialEntryRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            FinancialEntry? entry = await DbContext.Set<FinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureReferencesExist(request.AccountId, request.CampaignId, request.CampaignDeliverableId, cancellationToken);

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
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool accountExists = await DbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.AccountId && item.IsActive, cancellationToken);

            if (!accountExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

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

            return await GetEntryById(result.Id, cancellationToken) ?? result;
        }

        public async Task<FinancialSummaryModel> GetSummary(FinancialEntryType type, CancellationToken cancellationToken = default)
        {
            await RecalculateOverdueAsync(cancellationToken);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset firstDayOfMonth = new(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset next7Days = now.AddDays(7);

            List<FinancialEntry> entries = await DbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.Type == type)
                .ToListAsync(cancellationToken);

            return new FinancialSummaryModel
            {
                Type = type,
                TotalPending = entries.Where(item => item.Status == FinancialEntryStatus.Pending).Sum(item => item.Amount),
                TotalSettledThisMonth = entries.Where(item => item.Status == FinancialEntryStatus.Paid && item.PaidAt.HasValue && item.PaidAt.Value >= firstDayOfMonth).Sum(item => item.Amount),
                TotalOverdue = entries.Where(item => item.Status == FinancialEntryStatus.Overdue).Sum(item => item.Amount),
                TotalDueNext7Days = entries.Where(item => item.Status == FinancialEntryStatus.Pending && item.DueAt >= now && item.DueAt <= next7Days).Sum(item => item.Amount),
                PendingCount = entries.Count(item => item.Status == FinancialEntryStatus.Pending),
                OverdueCount = entries.Count(item => item.Status == FinancialEntryStatus.Overdue)
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

        private async Task EnsureReferencesExist(long accountId, long? campaignId, long? campaignDeliverableId, CancellationToken cancellationToken)
        {
            bool accountExists = await DbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == accountId && item.IsActive, cancellationToken);

            if (!accountExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (campaignId.HasValue)
            {
                bool campaignExists = await DbContext.Set<Campaign>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == campaignId.Value, cancellationToken);

                if (!campaignExists)
                {
                    throw new InvalidOperationException(localizer["record.notFound"]);
                }
            }

            if (campaignDeliverableId.HasValue)
            {
                bool deliverableExists = await DbContext.Set<CampaignDeliverable>()
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == campaignDeliverableId.Value, cancellationToken);

                if (!deliverableExists)
                {
                    throw new InvalidOperationException(localizer["record.notFound"]);
                }
            }
        }

        private IQueryable<FinancialEntry> QueryWithDetails()
        {
            return DbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Include(item => item.Account)
                .Include(item => item.Campaign)
                .Include(item => item.CampaignDeliverable);
        }
    }

    public sealed class FinancialAccountService : IFinancialAccountService
    {
        private readonly DbContext dbContext;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public FinancialAccountService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<FinancialAccountModel>> GetAll(bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<FinancialAccount> query = dbContext.Set<FinancialAccount>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            List<FinancialAccount> accounts = await query
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken);

            List<long> accountIds = accounts.Select(item => item.Id).ToList();

            var balances = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => accountIds.Contains(item.AccountId) && item.Status == FinancialEntryStatus.Paid)
                .GroupBy(item => new { item.AccountId, item.Type })
                .Select(group => new { group.Key.AccountId, group.Key.Type, Total = group.Sum(item => item.Amount) })
                .ToListAsync(cancellationToken);

            return accounts.Select(account =>
            {
                decimal received = balances.Where(b => b.AccountId == account.Id && b.Type == FinancialEntryType.Receivable).Sum(b => b.Total);
                decimal paid = balances.Where(b => b.AccountId == account.Id && b.Type == FinancialEntryType.Payable).Sum(b => b.Total);

                return new FinancialAccountModel
                {
                    Id = account.Id,
                    Name = account.Name,
                    Type = account.Type,
                    Bank = account.Bank,
                    Agency = account.Agency,
                    Number = account.Number,
                    InitialBalance = account.InitialBalance,
                    CurrentBalance = account.InitialBalance + received - paid,
                    Color = account.Color,
                    IsActive = account.IsActive
                };
            }).ToArray();
        }

        public async Task<FinancialAccountModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            var all = await GetAll(true, cancellationToken);
            return all.FirstOrDefault(item => item.Id == id);
        }

        public async Task<FinancialAccountModel> Create(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default)
        {
            FinancialAccount account = new(request.Name, request.Type, request.InitialBalance, request.Color, request.Bank, request.Agency, request.Number);
            dbContext.Set<FinancialAccount>().Add(account);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException(localizer["record.notFound"]);
        }

        public async Task<FinancialAccountModel> Update(long id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            account.Update(request.Name, request.Type, request.InitialBalance, request.Color, request.Bank, request.Agency, request.Number, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException(localizer["record.notFound"]);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool inUse = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .AnyAsync(item => item.AccountId == id, cancellationToken);

            if (inUse)
            {
                throw new InvalidOperationException("Conta possui lançamentos vinculados; inative ao invés de excluir.");
            }

            dbContext.Set<FinancialAccount>().Remove(account);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
