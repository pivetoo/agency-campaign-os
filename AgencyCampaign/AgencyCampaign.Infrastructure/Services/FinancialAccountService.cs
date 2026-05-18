using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialAccountService : IFinancialAccountService
    {
        private readonly DbContext dbContext;

        public FinancialAccountService(DbContext dbContext)
        {
            this.dbContext = dbContext;
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
                    IsActive = account.IsActive
                };
            }).ToArray();

            return new PagedResult<FinancialAccountModel>
            {
                Items = items,
                Pagination = paged.Pagination
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
                IsActive = account.IsActive
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
