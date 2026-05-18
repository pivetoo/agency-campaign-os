using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Clients;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialAccountService : IFinancialAccountService
    {
        private readonly DbContext dbContext;
        private readonly IntegrationPlatformClient integrationPlatformClient;

        public FinancialAccountService(DbContext dbContext, IntegrationPlatformClient integrationPlatformClient)
        {
            this.dbContext = dbContext;
            this.integrationPlatformClient = integrationPlatformClient;
        }

        public async Task<PagedResult<FinancialAccountModel>> GetAll(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            IQueryable<FinancialAccount> query = dbContext.Set<FinancialAccount>().AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower)
                    || (item.Bank != null && item.Bank.ToLower().Contains(lower)));
            }

            PagedResult<FinancialAccount> paged = await query
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Name)
                .ToPagedResultAsync(request, cancellationToken);

            List<long> accountIds = paged.Items.Select(item => item.Id).ToList();

            var balances = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => accountIds.Contains(item.AccountId) && item.Status == FinancialEntryStatus.Paid)
                .GroupBy(item => new { item.AccountId, item.Type })
                .Select(group => new { group.Key.AccountId, group.Key.Type, Total = group.Sum(item => item.Amount) })
                .ToListAsync(cancellationToken);

            HashSet<long> accountIdsWithEntries = (await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => accountIds.Contains(item.AccountId))
                .Select(item => item.AccountId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            FinancialAccountModel[] items = paged.Items.Select(account =>
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
                    IsActive = account.IsActive,
                    HasEntries = accountIdsWithEntries.Contains(account.Id),
                    IntegrationConnectorId = account.IntegrationConnectorId,
                    LastSyncedBalance = account.LastSyncedBalance,
                    LastSyncedAt = account.LastSyncedAt,
                    SyncStatus = account.SyncStatus
                };
            }).ToArray();

            return new PagedResult<FinancialAccountModel>
            {
                Items = items,
                Pagination = paged.Pagination
            };
        }

        public async Task<FinancialAccountSummaryModel> GetSummary(CancellationToken cancellationToken = default)
        {
            List<FinancialAccount> accounts = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<long> activeAccountIds = accounts.Where(item => item.IsActive).Select(item => item.Id).ToList();

            var balances = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => activeAccountIds.Contains(item.AccountId) && item.Status == FinancialEntryStatus.Paid)
                .GroupBy(item => new { item.AccountId, item.Type })
                .Select(group => new { group.Key.AccountId, group.Key.Type, Total = group.Sum(item => item.Amount) })
                .ToListAsync(cancellationToken);

            decimal totalKanvas = accounts.Where(item => item.IsActive).Sum(account =>
            {
                decimal received = balances.Where(b => b.AccountId == account.Id && b.Type == FinancialEntryType.Receivable).Sum(b => b.Total);
                decimal paid = balances.Where(b => b.AccountId == account.Id && b.Type == FinancialEntryType.Payable).Sum(b => b.Total);
                return account.InitialBalance + received - paid;
            });

            return new FinancialAccountSummaryModel
            {
                ActiveCount = accounts.Count(item => item.IsActive),
                InactiveCount = accounts.Count(item => !item.IsActive),
                TotalKanvasBalance = totalKanvas,
                TotalLastSyncedBalance = accounts.Where(item => item.IsActive && item.LastSyncedBalance.HasValue).Sum(item => item.LastSyncedBalance!.Value),
                SyncedAccountsCount = accounts.Count(item => item.IsActive && item.SyncStatus == FinancialAccountSyncStatus.Synced),
                PendingSyncAccountsCount = accounts.Count(item => item.IsActive && item.SyncStatus == FinancialAccountSyncStatus.Pending),
                ErroredSyncAccountsCount = accounts.Count(item => item.IsActive && item.SyncStatus == FinancialAccountSyncStatus.Error)
            };
        }

        public async Task<FinancialAccountModel?> GetById(long id, CancellationToken cancellationToken = default)
        {
            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                return null;
            }

            var balances = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.AccountId == id && item.Status == FinancialEntryStatus.Paid)
                .GroupBy(item => item.Type)
                .Select(group => new { Type = group.Key, Total = group.Sum(item => item.Amount) })
                .ToListAsync(cancellationToken);

            bool hasEntries = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .AnyAsync(item => item.AccountId == id, cancellationToken);

            decimal received = balances.Where(b => b.Type == FinancialEntryType.Receivable).Sum(b => b.Total);
            decimal paid = balances.Where(b => b.Type == FinancialEntryType.Payable).Sum(b => b.Total);

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
                IsActive = account.IsActive,
                HasEntries = hasEntries,
                IntegrationConnectorId = account.IntegrationConnectorId,
                LastSyncedBalance = account.LastSyncedBalance,
                LastSyncedAt = account.LastSyncedAt,
                SyncStatus = account.SyncStatus
            };
        }

        public async Task<FinancialAccountModel> Create(CreateFinancialAccountRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureNameIsUnique(request.Name, ignoreId: null, cancellationToken);

            FinancialAccount account = new(request.Name, request.Type, request.InitialBalance, request.Color, request.Bank, request.Agency, request.Number);
            dbContext.Set<FinancialAccount>().Add(account);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException("record.notFound");
        }

        public async Task<FinancialAccountModel> Update(long id, UpdateFinancialAccountRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            await EnsureNameIsUnique(request.Name, ignoreId: id, cancellationToken);

            if (request.InitialBalance != account.InitialBalance)
            {
                bool hasEntries = await dbContext.Set<FinancialEntry>()
                    .AsNoTracking()
                    .AnyAsync(item => item.AccountId == id, cancellationToken);

                if (hasEntries)
                {
                    throw new InvalidOperationException("financialAccount.initialBalance.lockedAfterEntries");
                }
            }

            account.Update(request.Name, request.Type, request.InitialBalance, request.Color, request.Bank, request.Agency, request.Number, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException("record.notFound");
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            bool inUse = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .AnyAsync(item => item.AccountId == id, cancellationToken);

            if (inUse)
            {
                throw new InvalidOperationException("financialAccount.hasEntries.cannotDelete");
            }

            dbContext.Set<FinancialAccount>().Remove(account);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<FinancialAccountModel> AttachConnector(long id, long connectorId, CancellationToken cancellationToken = default)
        {
            FinancialAccount account = await LoadTracked(id, cancellationToken);
            account.AttachConnector(connectorId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException("record.notFound");
        }

        public async Task<FinancialAccountModel> DetachConnector(long id, CancellationToken cancellationToken = default)
        {
            FinancialAccount account = await LoadTracked(id, cancellationToken);
            account.DetachConnector();
            await dbContext.SaveChangesAsync(cancellationToken);
            return await GetById(account.Id, cancellationToken) ?? throw new InvalidOperationException("record.notFound");
        }

        public async Task<long> TriggerSync(long id, CancellationToken cancellationToken = default)
        {
            FinancialAccount account = await LoadTracked(id, cancellationToken);
            if (account.IntegrationConnectorId is null)
            {
                throw new InvalidOperationException("financialAccount.sync.noConnector");
            }

            account.MarkSyncPending();
            await dbContext.SaveChangesAsync(cancellationToken);

            string payload = $"{{\"financialAccountId\":{account.Id}}}";
            try
            {
                ExecutionDto execution = await integrationPlatformClient.ExecuteDefaultPipelineAsync(account.IntegrationConnectorId.Value, payload, cancellationToken);
                return execution.Id;
            }
            catch
            {
                account.MarkSyncError(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(CancellationToken.None);
                throw;
            }
        }

        private async Task<FinancialAccount> LoadTracked(long id, CancellationToken cancellationToken)
        {
            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return account;
        }

        private async Task EnsureNameIsUnique(string name, long? ignoreId, CancellationToken cancellationToken)
        {
            string normalized = name.Trim().ToLower();

            bool exists = await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .AnyAsync(item => item.Name.ToLower() == normalized && (ignoreId == null || item.Id != ignoreId), cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("financialAccount.name.duplicated");
            }
        }
    }
}
